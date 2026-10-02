using Talby.Core.ResxAccess;

namespace Consumer.Api;

// The standard snapshot runner does not forward the SDK project path or resource map.
// A real consumer build covers generation and resource lookup with these files.
[GenerateResxAccess("Resources/Labels.resx")]
internal static class Texts
{
}
