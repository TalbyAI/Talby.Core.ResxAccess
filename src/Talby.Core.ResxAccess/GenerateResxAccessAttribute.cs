using System.Globalization;
using System.Resources;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Talby.Core.ResxAccess;

/// <summary>Validates a Resource Set and introduces Raw Text and Indexed Placeholder formatting methods.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateResxAccessAttribute : TypeAspect
{
    private readonly string _referenceResource;

    /// <summary>Requires a Localized Resource for each culture name, in addition to validating all discovered cultures.</summary>
    public string[]? ExpectedCultures { get; set; }

    public GenerateResxAccessAttribute(string referenceResource)
    {
        _referenceResource = referenceResource;
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var project = builder.Target.Compilation.Project;
        project.TryGetProperty("TalbyResxResourceMap", out var resourceMap);
        ResxAccessImplementation.Build(builder, _referenceResource, project.Path, resourceMap, ExpectedCultures);
    }

    [Template]
    private static readonly ResourceManager __resxResourceManager = new((string)meta.Tags["ManifestBaseName"]!, meta.Target.Type.ToType().Assembly);

    [Template]
    public static string RawText([CompileTime] IMethod cultureMethod)
        => cultureMethod.Invoke(CultureInfo.CurrentUICulture)!;

    [Template]
    public static string RawTextWithCulture(CultureInfo resourceCulture, [CompileTime] string key, [CompileTime] IField resourceManagerField)
    {
        ArgumentNullException.ThrowIfNull(resourceCulture);
        var resourceManager = (ResourceManager)resourceManagerField.Value!;
        try
        {
            return resourceManager.GetString(key, resourceCulture)
                ?? throw new InvalidOperationException($"Resource Key '{key}' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'.");
        }
        catch (MissingManifestResourceException exception)
        {
            throw new InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.", exception);
        }
        catch (MissingSatelliteAssemblyException exception)
        {
            throw new InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.", exception);
        }
    }

    [Template]
    public static string FormattedText([CompileTime] IMethod rawTextMethod, [CompileTime] int[] indices, [CompileTime] int cultureCount)
    {
        var resourceCulture = CultureInfo.CurrentUICulture;
        var formattingCulture = CultureInfo.CurrentCulture;
        if (cultureCount >= 1)
        {
            resourceCulture = (CultureInfo)meta.Target.Parameters[indices.Length].Value!;
        }
        if (cultureCount == 2)
        {
            formattingCulture = (CultureInfo)meta.Target.Parameters[indices.Length + 1].Value!;
            ArgumentNullException.ThrowIfNull(formattingCulture);
        }

        var text = (string)rawTextMethod.Invoke(resourceCulture)!;
        var arguments = new object?[indices.Last() + 1];
        foreach (var index in indices)
        {
            arguments[index] = meta.Target.Parameters["arg" + index].Value;
        }
        return string.Format(formattingCulture, text, arguments);
    }
}
