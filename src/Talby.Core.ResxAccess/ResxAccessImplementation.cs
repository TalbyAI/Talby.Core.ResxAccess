using System.Globalization;
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

    public static void Build(
        IAspectBuilder<INamedType> builder,
        string referenceResource,
        string? projectPath,
        string? resourceMap,
        string[]? expectedCultures = null
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
            var adviser = builder.WithTemplateProvider(
                new GenerateResxAccessAttribute(referenceResource)
            );
            var resourceManagerField = adviser
                .IntroduceField(
                    "__resxResourceManager",
                    tags: new { ManifestBaseName = resource.ManifestBaseName }
                )
                .Declaration;
            foreach (var key in resource.Keys)
            {
                if (!ReferenceResourceReader.IsResourceKeyIdentifier(key))
                {
                    continue;
                }

                var cultureMethod = adviser
                    .IntroduceMethod(
                        nameof(GenerateResxAccessAttribute.RawTextWithCulture),
                        buildMethod: method => method.Name = key,
                        args: new { key, resourceManagerField }
                    )
                    .Declaration;
                adviser.IntroduceMethod(
                    nameof(GenerateResxAccessAttribute.RawText),
                    buildMethod: method => method.Name = key,
                    args: new { cultureMethod }
                );
                if (
                    resource.IndexedArguments.TryGetValue(key, out var indices)
                    && indices.Length > 0
                )
                {
                    for (var cultureCount = 0; cultureCount <= 2; cultureCount++)
                    {
                        var overloadCultureCount = cultureCount;
                        adviser.IntroduceMethod(
                            nameof(GenerateResxAccessAttribute.FormattedText),
                            buildMethod: method =>
                            {
                                method.Name = "Format" + key;
                                foreach (var index in indices)
                                {
                                    method.AddParameter(
                                        "arg" + index,
                                        TypeFactory.GetType(typeof(object)).ToNullable()
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
                                indices,
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
}
