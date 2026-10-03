using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal sealed class ResourceValidationException : Exception
{
    public bool UnsupportedEmbedding { get; }

    public ResourceValidationException(string message, bool unsupportedEmbedding = false) : base(message)
    {
        UnsupportedEmbedding = unsupportedEmbedding;
    }
}
