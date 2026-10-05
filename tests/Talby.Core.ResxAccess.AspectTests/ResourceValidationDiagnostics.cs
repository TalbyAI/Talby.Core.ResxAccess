#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/PlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

// Reference Resource structure, embedding metadata and unsupported targets.
[DeterministicResxAccess("<resources />")]
internal static class InvalidRoot { }

[DeterministicResxAccess("<root><data name=\"Text\" /></root>")]
internal static class InvalidEntry { }

[DeterministicResxAccess("<root />", "Name|Custom|||false")]
internal static class CustomEmbedding { }

[DeterministicResxAccess("<root />", "Name||||true")]
internal static class CultureSpecificEmbedding { }

[DeterministicResxAccess("<root />", "||||false")]
internal static class NotEmbedded { }

internal class GenericContainer<T>
{
    [DeterministicResxAccess("<root />")]
    internal static class NestedTarget { }
}

// Reference Resource and Localized Resource entry validation.
[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value>One</value></data><data name=\"omitted-key\"><value>Two</value></data></root>"
)]
internal static class DuplicateReferenceKey { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\" type=\"System.Int32, mscorlib\"><value>1</value></data></root>"
)]
internal static class NonTextReferenceEntry { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value>Reference</value></data></root>",
    localizedXml: "<root><data name=\"omitted-key\"><value>One</value></data><data name=\"omitted-key\"><value>Two</value></data></root>"
)]
internal static class DuplicateLocalizedKey { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value>Reference</value></data></root>",
    localizedXml: "<root><data name=\"omitted-key\" type=\"System.Int32, mscorlib\"><value>1</value></data></root>"
)]
internal static class NonTextLocalizedEntry { }

[DeterministicResxAccess("<root />", localizedXml: "<resources />")]
internal static class InvalidLocalizedRoot { }

[DeterministicResxAccess("<root />", localizedXml: "<root><data>")]
internal static class MalformedLocalizedXml { }

// Localized Resource key consistency.
[DeterministicResxAccess(
    "<root><data name=\"Text\"><value>Reference</value></data></root>",
    localizedXml: "<root />"
)]
internal static class MissingKey { }

[DeterministicResxAccess(
    "<root />",
    localizedXml: "<root><data name=\"Extra\"><value>Translation</value></data></root>"
)]
internal static class AdditionalKey { }

[DeterministicResxAccess(
    "<root><data name=\"Text\"><value>Reference</value></data></root>",
    localizedXml: "<root><data name=\"text\"><value>Translation</value></data></root>"
)]
internal static class CaseMismatchedKey { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value>Reference</value></data></root>",
    localizedXml: "<root />"
)]
internal static class OmittedKey { }

// Expected Culture validation.
[DeterministicResxAccess(
    "<root />",
    localizedXml: "<root />",
    ExpectedCultures = new[] { "es", "fr" }
)]
internal static class MissingExpectedCulture { }

[DeterministicResxAccess("<root />", ExpectedCultures = new[] { "not-a-culture" })]
internal static class InvalidExpectedCulture { }

[DeterministicResxAccess("<root />", ExpectedCultures = new[] { "" })]
internal static class InvariantExpectedCulture { }

[DeterministicResxAccess("<root />", ExpectedCultures = new string[] { null! })]
internal static class NullExpectedCulture { }

// Indexed Placeholder Contract validation.
[DeterministicResxAccess("<root><data name=\"Bad\"><value>{0,}</value></data></root>")]
internal static class MalformedReference { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{0} {2}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{0}</value></data></root>"
)]
internal static class MissingIdentity { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value /></data></root>",
    localizedXml: "<root><data name=\"omitted-key\"><value>{0}</value></data></root>"
)]
internal static class AdditionalIdentityOnOmittedKey { }

[DeterministicResxAccess(
    "<root><data name=\"Bad\"><value>{0}</value></data></root>",
    localizedXml: "<root><data name=\"Bad\"><value>{0</value></data></root>"
)]
internal static class MalformedLocalized { }
