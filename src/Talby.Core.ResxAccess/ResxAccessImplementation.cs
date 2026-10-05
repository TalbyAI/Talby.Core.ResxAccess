using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal static class ResxAccessImplementation
{
    private static readonly DiagnosticDefinition<string> InvalidReference = new(
        "TRESX001",
        Severity.Error,
        "Invalid Reference Resource: {0}",
        "Invalid Reference Resource"
    );

    private static readonly DiagnosticDefinition<string> UnsupportedTarget = new(
        "TRESX002",
        Severity.Error,
        "Resource Access requires a non-generic static class: {0}",
        "Unsupported Resource Access target"
    );

    private static readonly DiagnosticDefinition<string> UnsupportedEmbedding = new(
        "TRESX003",
        Severity.Error,
        "Unsupported Reference Resource embedding: {0}",
        "Unsupported resource embedding"
    );

    private static readonly DiagnosticDefinition<string> InvalidLocalized = new(
        "TRESX004",
        Severity.Error,
        "Invalid Localized Resource: {0}",
        "Invalid Localized Resource"
    );

    private static readonly DiagnosticDefinition<string> InvalidExpectedCultures = new(
        "TRESX005",
        Severity.Error,
        "Invalid ExpectedCultures: {0}",
        "Invalid ExpectedCultures"
    );

    private static readonly DiagnosticDefinition<(string Key, string Resource)> InvalidIdentifier =
        new(
            "TRESX006",
            Severity.Warning,
            "Resource Key '{0}' in '{1}' cannot become a C# method identifier; its members are omitted.",
            "Invalid Resource Key identifier"
        );

    private static readonly DiagnosticDefinition<string> MemberCollision = new(
        "TRESX007",
        Severity.Error,
        "Resource Access member collision: {0}",
        "Resource Access member collision"
    );

    private static readonly DiagnosticDefinition<int> InvalidIdentifierPolicy = new(
        "TRESX008",
        Severity.Error,
        "InvalidKeyHandling value '{0}' is unsupported. Specify Warn, Ignore, or Normalize.",
        "Invalid Resource Key identifier policy"
    );

    public static void Build(
        IAspectBuilder<INamedType> builder,
        string referenceResource,
        string? projectPath,
        string? resourceMap,
        string[]? expectedCultures = null,
        InvalidKeyHandling invalidKeyHandling = InvalidKeyHandling.Warn
    )
    {
        for (var type = builder.Target; type is not null; type = type.DeclaringType)
        {
            if (
                type.TypeParameters.Count > 0
                || (type == builder.Target && (!type.IsStatic || type.TypeKind != TypeKind.Class))
            )
            {
                builder.Diagnostics.Report(
                    UnsupportedTarget.WithArguments(builder.Target.ToDisplayString())
                );
                return;
            }
        }

        try
        {
            var resource = ReferenceResourceReader.Read(
                referenceResource,
                projectPath,
                resourceMap,
                expectedCultures
            );
            if (
                invalidKeyHandling != InvalidKeyHandling.Warn
                && invalidKeyHandling != InvalidKeyHandling.Ignore
                && invalidKeyHandling != InvalidKeyHandling.Normalize
            )
            {
                builder.Diagnostics.Report(
                    InvalidIdentifierPolicy.WithArguments((int)invalidKeyHandling)
                );
                return;
            }
            var identifiers = AssignIdentifiers(resource.Keys, invalidKeyHandling);
            foreach (var key in resource.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                if (!identifiers.ContainsKey(key) && invalidKeyHandling == InvalidKeyHandling.Warn)
                {
                    builder.Diagnostics.Report(
                        InvalidIdentifier.WithArguments(
                            (PlaceholderContract.DescribeIdentifier(key), referenceResource)
                        )
                    );
                }
            }
            if (
                HasMemberCollisions(
                    builder,
                    referenceResource,
                    identifiers,
                    resource.PlaceholderContracts
                )
            )
            {
                return;
            }
            var adviser = builder.WithTemplateProvider(
                new GenerateResxAccessAttribute(referenceResource)
            );
            var resourceManagerField = adviser
                .IntroduceField(
                    "__resxResourceManager",
                    tags: new { ManifestBaseName = resource.ManifestBaseName }
                )
                .Declaration;
            foreach (var key in resource.Keys.OrderBy(key => key, StringComparer.Ordinal))
            {
                if (!identifiers.TryGetValue(key, out var identifier))
                {
                    continue;
                }

                // C# ignores formatting characters, including when resolving consumer calls.
                var memberName = IdentifierIdentity(identifier);

                var cultureMethod = adviser
                    .IntroduceMethod(
                        nameof(GenerateResxAccessAttribute.RawTextWithCulture),
                        buildMethod: method => method.Name = memberName,
                        args: new { key, resourceManagerField }
                    )
                    .Declaration;
                adviser.IntroduceMethod(
                    nameof(GenerateResxAccessAttribute.RawText),
                    buildMethod: method => method.Name = memberName,
                    args: new { cultureMethod }
                );
                if (
                    resource.PlaceholderContracts.TryGetValue(key, out var contract)
                    && contract.Arguments.Length > 0
                )
                {
                    for (var cultureCount = 0; cultureCount <= 2; cultureCount++)
                    {
                        var overloadCultureCount = cultureCount;
                        adviser.IntroduceMethod(
                            nameof(GenerateResxAccessAttribute.FormattedText),
                            buildMethod: method =>
                            {
                                method.Name = "Format" + memberName;
                                foreach (var argument in contract.Arguments)
                                {
                                    var type = TypeFactory.GetType(argument.Type);
                                    method.AddParameter(
                                        argument.Name,
                                        argument.TypeName.EndsWith("?", StringComparison.Ordinal)
                                            ? type.ToNullable()
                                            : type.ToNonNullable()
                                    );
                                }
                                if (overloadCultureCount >= 1)
                                {
                                    method.AddParameter("resourceCulture", typeof(CultureInfo));
                                }
                                if (overloadCultureCount == 2)
                                {
                                    method.AddParameter("formattingCulture", typeof(CultureInfo));
                                }
                            },
                            args: new
                            {
                                rawTextMethod = cultureMethod,
                                parameterNames = contract
                                    .Arguments.Select(argument => argument.Name)
                                    .ToArray(),
                                formats = contract.Formats,
                                cultureCount,
                            }
                        );
                    }
                }
            }
        }
        catch (ResourceValidationException exception)
        {
            var diagnostic =
                exception.InvalidExpectedCultures ? InvalidExpectedCultures
                : exception.LocalizedResource ? InvalidLocalized
                : exception.UnsupportedEmbedding ? UnsupportedEmbedding
                : InvalidReference;
            builder.Diagnostics.Report(diagnostic.WithArguments(exception.Message));
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or XmlException
                        or ArgumentException
            )
        {
            builder.Diagnostics.Report(
                InvalidReference.WithArguments(
                    $"'{referenceResource}' could not be read: {exception.Message}"
                )
            );
        }
    }

    private static Dictionary<string, string> AssignIdentifiers(
        IEnumerable<string> keys,
        InvalidKeyHandling invalidKeyHandling
    )
    {
        var identifiers = keys.Where(ReferenceResourceReader.IsResourceKeyIdentifier)
            .ToDictionary(key => key, key => key, StringComparer.Ordinal);
        if (invalidKeyHandling != InvalidKeyHandling.Normalize)
        {
            return identifiers;
        }

        var reserved = new HashSet<string>(
            identifiers.Values.Select(IdentifierIdentity),
            StringComparer.Ordinal
        );
        foreach (
            var key in keys.Where(key => !identifiers.ContainsKey(key))
                .OrderBy(key => key, StringComparer.Ordinal)
        )
        {
            var normalized = Regex.Replace(
                key,
                @"[^_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]",
                "_"
            );
            normalized = Regex.Replace(normalized, @"\A[^_\p{L}\p{Nl}]", "_");
            var identifier = normalized;
            for (var suffix = 2; !reserved.Add(IdentifierIdentity(identifier)); suffix++)
            {
                identifier = normalized + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            }
            identifiers.Add(key, identifier);
        }
        return identifiers;
    }

    private static string IdentifierIdentity(string identifier) =>
        Regex.Replace(identifier, @"\p{Cf}", "");

    private static bool HasMemberCollisions(
        IAspectBuilder<INamedType> builder,
        string referenceResource,
        Dictionary<string, string> identifiers,
        Dictionary<string, PlaceholderContract> contracts
    )
    {
        var target = builder.Target;
        var existing = new HashSet<string>(
            target
                .Methods.Select(member => member.Name)
                .Concat(target.FieldsAndProperties.Select(member => member.Name))
                .Concat(target.Events.Select(member => member.Name))
                .Concat(target.Types.Select(member => member.Name))
                .Append(target.Name)
                .Select(IdentifierIdentity),
            StringComparer.Ordinal
        );
        var collision = false;
        if (existing.Contains("__resxResourceManager"))
        {
            builder.Diagnostics.Report(
                MemberCollision.WithArguments(
                    $"Generated ResourceManager field '__resxResourceManager' in '{referenceResource}' collides with an existing target-class member."
                )
            );
            collision = true;
        }
        var families = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["__resxResourceManager"] = "the generated ResourceManager field",
        };
        foreach (var entry in identifiers.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var key = entry.Key;
            var identifier = entry.Value;
            var names = new List<string> { identifier };
            if (contracts[key].Arguments.Length > 0)
            {
                names.Add("Format" + identifier);
            }
            foreach (var name in names)
            {
                var identity = IdentifierIdentity(name);
                var conflict =
                    existing.Contains(identity) ? "an existing target-class member"
                    : families.TryGetValue(identity, out var family) ? family
                    : null;
                if (conflict is not null)
                {
                    builder.Diagnostics.Report(
                        MemberCollision.WithArguments(
                            $"Resource Key '{PlaceholderContract.DescribeIdentifier(key)}' in '{referenceResource}' generates member '{PlaceholderContract.DescribeIdentifier(name)}', which collides with {conflict}."
                        )
                    );
                    collision = true;
                }
                else
                {
                    families.Add(
                        identity,
                        $"the member family for Resource Key '{PlaceholderContract.DescribeIdentifier(key)}'"
                    );
                }
            }
        }
        return collision;
    }
}
