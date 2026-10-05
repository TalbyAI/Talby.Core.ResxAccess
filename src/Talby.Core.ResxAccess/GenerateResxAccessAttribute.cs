using System.Globalization;
using System.Resources;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Talby.Core.ResxAccess;

/// <summary>Validates a Resource Set and introduces Raw Text and formatting methods.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateResxAccessAttribute : TypeAspect
{
    private readonly string _referenceResource;

    /// <summary>Requires a Localized Resource for each culture name, in addition to validating all discovered cultures.</summary>
    public string[]? ExpectedCultures { get; set; }

    /// <summary>Controls invalid Resource Key identifiers without disabling Resource Set validation.</summary>
    public InvalidKeyHandling InvalidKeyHandling { get; set; } = InvalidKeyHandling.Warn;

    public GenerateResxAccessAttribute(string referenceResource)
    {
        _referenceResource = referenceResource;
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        // Reading the generated type registers a Metalama dependency before validation,
        // including when the Resource Set is invalid and no methods can be introduced.
        if (
            builder.Target.Compilation.Factory.TryGetTypeByReflectionName(
                "Talby.Core.ResxAccess.Generated.ResourceDependency",
                out var dependency
            )
        )
        {
            _ = dependency.Fields.Count;
        }
        var project = builder.Target.Compilation.Project;
        project.TryGetProperty("TalbyResxResourceMap", out var resourceMap);
        ResxAccessImplementation.Build(
            builder,
            _referenceResource,
            project.Path,
            resourceMap,
            ExpectedCultures,
            InvalidKeyHandling
        );
    }

    [Template]
    private static readonly ResourceManager __resxResourceManager = new(
        (string)meta.Tags["ManifestBaseName"]!,
        meta.Target.Type.ToType().Assembly
    );

    [Template]
    public static string RawText([CompileTime] IMethod cultureMethod) =>
        cultureMethod.Invoke(CultureInfo.CurrentUICulture)!;

    [Template]
    public static string RawTextWithCulture(
        CultureInfo resourceCulture,
        [CompileTime] string key,
        [CompileTime] IField resourceManagerField
    )
    {
        ArgumentNullException.ThrowIfNull(resourceCulture);
        var resourceManager = (ResourceManager)resourceManagerField.Value!;
        try
        {
            return resourceManager.GetString(key, resourceCulture)
                ?? throw new InvalidOperationException(
                    $"Resource Key '{key}' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'."
                );
        }
        catch (MissingManifestResourceException exception)
        {
            throw new InvalidOperationException(
                $"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.",
                exception
            );
        }
        catch (MissingSatelliteAssemblyException exception)
        {
            throw new InvalidOperationException(
                $"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.",
                exception
            );
        }
    }

    [Template]
    public static string FormattedText(
        [CompileTime] IMethod rawTextMethod,
        [CompileTime] string[] parameterNames,
        [CompileTime] Dictionary<string, string> formats,
        [CompileTime] int cultureCount
    )
    {
        var resourceCulture = CultureInfo.CurrentUICulture;
        var formattingCulture = CultureInfo.CurrentCulture;
        if (cultureCount >= 1)
        {
            resourceCulture = (CultureInfo)meta.Target.Parameters[parameterNames.Length].Value!;
        }
        if (cultureCount == 2)
        {
            formattingCulture = (CultureInfo)
                meta.Target.Parameters[parameterNames.Length + 1].Value!;
            ArgumentNullException.ThrowIfNull(formattingCulture);
        }

        var text = (string)rawTextMethod.Invoke(resourceCulture)!;
        var arguments = new object?[parameterNames.Length];
        foreach (var index in meta.CompileTime(Enumerable.Range(0, parameterNames.Length)))
        {
            arguments[index] = meta.Target.Parameters[parameterNames[index]].Value;
        }
        foreach (var format in formats)
        {
            if (text == format.Key)
            {
                return string.Format(formattingCulture, format.Value, arguments);
            }
        }
        return string.Format(formattingCulture, text, arguments);
    }
}
