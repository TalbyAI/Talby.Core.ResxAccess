using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal sealed class ResourceValidationException : Exception
{
    public bool UnsupportedEmbedding { get; }
    public bool LocalizedResource { get; }
    public bool InvalidExpectedCultures { get; }

    public ResourceValidationException(
        string message,
        bool unsupportedEmbedding = false,
        bool localizedResource = false,
        bool invalidExpectedCultures = false
    )
        : base(message)
    {
        UnsupportedEmbedding = unsupportedEmbedding;
        LocalizedResource = localizedResource;
        InvalidExpectedCultures = invalidExpectedCultures;
    }
}
