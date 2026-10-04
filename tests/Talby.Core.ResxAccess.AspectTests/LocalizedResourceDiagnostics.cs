#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/IndexedPlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess("<root><data name=\"Text\"><value>Reference</value></data></root>", localizedXml: "<root />")]
internal static class MissingKey
{
}

[DeterministicResxAccess("<root />", localizedXml: "<root><data name=\"Extra\"><value>Translation</value></data></root>")]
internal static class AdditionalKey
{
}

[DeterministicResxAccess("<root><data name=\"Text\"><value>Reference</value></data></root>", localizedXml: "<root><data name=\"text\"><value>Translation</value></data></root>")]
internal static class CaseMismatchedKey
{
}

[DeterministicResxAccess("<root><data name=\"omitted-key\"><value>Reference</value></data></root>", localizedXml: "<root />")]
internal static class OmittedKey
{
}
