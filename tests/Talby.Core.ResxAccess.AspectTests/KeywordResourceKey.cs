#if TEST_OPTIONS
// @TestScenario(DesignTime)
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/IndexedPlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

// The SDK fixture checks the reserved-keyword method bodies and runtime lookup.
// DesignTime checks declarations without the snapshot runner's invocation formatting.
[DeterministicResxAccess("<root><data name=\"class\"><value>Keyword</value></data></root>")]
internal static partial class KeywordTexts { }
