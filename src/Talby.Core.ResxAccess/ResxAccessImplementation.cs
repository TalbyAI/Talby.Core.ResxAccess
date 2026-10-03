using System.Xml;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal static class ResxAccessImplementation
{
    private static readonly DiagnosticDefinition<string> InvalidReference = new(
        "TRESX001", Severity.Error, "Invalid Reference Resource: {0}", "Invalid Reference Resource");

    private static readonly DiagnosticDefinition<string> UnsupportedTarget = new(
        "TRESX002", Severity.Error, "Resource Access requires a non-generic static class: {0}", "Unsupported Resource Access target");

    private static readonly DiagnosticDefinition<string> UnsupportedEmbedding = new(
        "TRESX003", Severity.Error, "Unsupported Reference Resource embedding: {0}", "Unsupported resource embedding");

    public static void Build(IAspectBuilder<INamedType> builder, string referenceResource, string? projectPath, string? resourceMap)
    {
        for (var type = builder.Target; type is not null; type = type.DeclaringType)
        {
            if (type.TypeParameters.Count > 0 || (type == builder.Target && (!type.IsStatic || type.TypeKind != TypeKind.Class)))
            {
                builder.Diagnostics.Report(UnsupportedTarget.WithArguments(builder.Target.ToDisplayString()));
                return;
            }
        }

        try
        {
            var resource = ReferenceResourceReader.Read(referenceResource, projectPath, resourceMap);
            var adviser = builder.WithTemplateProvider(new GenerateResxAccessAttribute(referenceResource));
            var resourceManagerField = adviser.IntroduceField("__resxResourceManager", tags: new { ManifestBaseName = resource.ManifestBaseName }).Declaration;
            foreach (var key in resource.Keys)
            {
                if (!ReferenceResourceReader.IsResourceKeyIdentifier(key))
                {
                    continue;
                }

                var cultureMethod = adviser.IntroduceMethod(nameof(GenerateResxAccessAttribute.RawTextWithCulture), buildMethod: method => method.Name = key, args: new { key, resourceManagerField }).Declaration;
                adviser.IntroduceMethod(nameof(GenerateResxAccessAttribute.RawText), buildMethod: method => method.Name = key, args: new { cultureMethod });
            }
        }
        catch (ResourceValidationException exception)
        {
            builder.Diagnostics.Report((exception.UnsupportedEmbedding ? UnsupportedEmbedding : InvalidReference).WithArguments(exception.Message));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' could not be read: {exception.Message}"));
        }
    }
}
