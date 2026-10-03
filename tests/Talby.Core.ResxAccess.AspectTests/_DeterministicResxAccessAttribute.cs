using System;
using System.IO;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Talby.Core.ResxAccess;

namespace Consumer.Api;

// Bypasses only SDK project/property acquisition and SDK resource-map creation.
internal sealed class DeterministicResxAccessAttribute : TypeAspect
{
    private readonly string _xml;
    private readonly string _metadata;

    public DeterministicResxAccessAttribute(string xml, string metadata = "ConsumerRoot.Resources.Labels||||false")
    {
        _xml = xml;
        _metadata = metadata;
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ResxAccessAspectTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var resourcePath = Path.Combine(directory, "Labels.resx");
            var mapPath = Path.Combine(directory, "resources.txt");
            File.WriteAllText(resourcePath, _xml);
            File.WriteAllText(mapPath, $"{resourcePath}|{_metadata}");
            ResxAccessImplementation.Build(builder, "Labels.resx", Path.Combine(directory, "Consumer.csproj"), mapPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
