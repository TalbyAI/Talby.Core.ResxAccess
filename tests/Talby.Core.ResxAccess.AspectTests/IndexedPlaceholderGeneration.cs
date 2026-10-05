#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/IndexedPlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{2} / {0:N2} / {2}</value></data><data name=\"Literal\"><value>{{0}}</value></data></root>"
)]
internal static class IndexedTexts { }
