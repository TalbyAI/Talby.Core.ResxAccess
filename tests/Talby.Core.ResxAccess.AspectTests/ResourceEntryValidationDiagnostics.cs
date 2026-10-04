#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/IndexedPlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess("<root><data name=\"omitted-key\"><value>One</value></data><data name=\"omitted-key\"><value>Two</value></data></root>")]
internal static class DuplicateReferenceKey
{
}

[DeterministicResxAccess("<root><data name=\"omitted-key\" type=\"System.Int32, mscorlib\"><value>1</value></data></root>")]
internal static class NonTextReferenceEntry
{
}

[DeterministicResxAccess("<root><data name=\"omitted-key\"><value>Reference</value></data></root>", localizedXml: "<root><data name=\"omitted-key\"><value>One</value></data><data name=\"omitted-key\"><value>Two</value></data></root>")]
internal static class DuplicateLocalizedKey
{
}

[DeterministicResxAccess("<root><data name=\"omitted-key\"><value>Reference</value></data></root>", localizedXml: "<root><data name=\"omitted-key\" type=\"System.Int32, mscorlib\"><value>1</value></data></root>")]
internal static class NonTextLocalizedEntry
{
}

[DeterministicResxAccess("<root />", localizedXml: "<resources />")]
internal static class InvalidLocalizedRoot
{
}

[DeterministicResxAccess("<root />", localizedXml: "<root><data>")]
internal static class MalformedLocalizedXml
{
}
