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
    private readonly string? _localizedXml;

    public string[]? ExpectedCultures { get; set; }

    public DeterministicResxAccessAttribute(string xml, string metadata = "ConsumerRoot.Resources.Labels||||false", string? localizedXml = null)
    {
        _xml = xml;
        _metadata = metadata;
        _localizedXml = localizedXml;
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
            if (_localizedXml is not null)
            {
                var localizedPath = Path.Combine(directory, "Labels.es.resx");
                File.WriteAllText(localizedPath, _localizedXml);
                File.AppendAllText(mapPath, $"\n{localizedPath}|{_metadata.Split(new[] { '|' })[0]}.es||||true");
            }
            ResxAccessImplementation.Build(builder, "Labels.resx", Path.Combine(directory, "Consumer.csproj"), mapPath, ExpectedCultures);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
