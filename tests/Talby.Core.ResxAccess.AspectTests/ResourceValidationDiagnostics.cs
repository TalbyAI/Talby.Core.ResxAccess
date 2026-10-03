#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess("<resources />")]
internal static class InvalidRoot
{
}

[DeterministicResxAccess("<root><data name=\"Text\" /></root>")]
internal static class InvalidEntry
{
}

[DeterministicResxAccess("<root />", "Name|Custom|||false")]
internal static class CustomEmbedding
{
}

[DeterministicResxAccess("<root />", "Name||||true")]
internal static class CultureSpecificEmbedding
{
}

[DeterministicResxAccess("<root />", "||||false")]
internal static class NotEmbedded
{
}

internal class GenericContainer<T>
{
    [DeterministicResxAccess("<root />")]
    internal static class NestedTarget
    {
    }
}
