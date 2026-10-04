#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/IndexedPlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{0,}</value></data></root>")]
internal static class MalformedReference
{
}

[DeterministicResxAccess("<root><data name=\"Summary\"><value>{0} {2}</value></data></root>", localizedXml: "<root><data name=\"Summary\"><value>{0}</value></data></root>")]
internal static class MissingIdentity
{
}

[DeterministicResxAccess("<root><data name=\"omitted-key\"><value /></data></root>", localizedXml: "<root><data name=\"omitted-key\"><value>{0}</value></data></root>")]
internal static class AdditionalIdentityOnOmittedKey
{
}

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{0}</value></data></root>", localizedXml: "<root><data name=\"Bad\"><value>{0</value></data></root>")]
internal static class MalformedLocalized
{
}
