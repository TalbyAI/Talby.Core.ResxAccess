#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

[DeterministicResxAccess("<root />", localizedXml: "<root />", ExpectedCultures = new[] { "es", "fr" })]
internal static class MissingExpectedCulture
{
}

[DeterministicResxAccess("<root />", ExpectedCultures = new[] { "not-a-culture" })]
internal static class InvalidExpectedCulture
{
}

[DeterministicResxAccess("<root />", ExpectedCultures = new[] { "" })]
internal static class InvariantExpectedCulture
{
}

[DeterministicResxAccess("<root />", ExpectedCultures = new string[] { null! })]
internal static class NullExpectedCulture
{
}
