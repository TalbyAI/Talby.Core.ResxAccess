using System.Globalization;
using System.Resources;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Talby.Core.ResxAccess;

/// <summary>Introduces Raw Text methods for a culture-neutral, SDK-embedded .resx file.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateResxAccessAttribute : TypeAspect
{
    private readonly string _referenceResource;

    public GenerateResxAccessAttribute(string referenceResource)
    {
        _referenceResource = referenceResource;
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var project = builder.Target.Compilation.Project;
        project.TryGetProperty("TalbyResxResourceMap", out var resourceMap);
        ResxAccessImplementation.Build(builder, _referenceResource, project.Path, resourceMap);
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
}
