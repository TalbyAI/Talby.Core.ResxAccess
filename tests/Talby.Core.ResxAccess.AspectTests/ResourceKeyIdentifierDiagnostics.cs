#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/PlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
using Talby.Core.ResxAccess;

namespace Consumer.Api;

// Existing members and generated member families.
[DeterministicResxAccess("<root><data name=\"Plain\"><value>Text</value></data></root>")]
internal static class ExistingRawMember
{
    public static string Plain() => "Existing";
}

[DeterministicResxAccess(
    "<root><data name=\"has-dash\"><value>{name@string}</value></data></root>",
    InvalidKeyHandling = InvalidKeyHandling.Normalize
)]
internal static class ExistingNormalizedFormattingMember
{
    public static string Formathas_dash(string name) => name;
}

[DeterministicResxAccess(
    "<root><data name=\"Plain\"><value>{0}</value></data><data name=\"FormatPlain\"><value>Text</value></data></root>"
)]
internal static class GeneratedMemberFamilies { }

[DeterministicResxAccess(
    "<root><data name=\"Plain!\"><value>{name}</value></data><data name=\"FormatPlain!\"><value>Text</value></data></root>",
    InvalidKeyHandling = InvalidKeyHandling.Normalize
)]
internal static class NormalizedMemberFamilies { }

[DeterministicResxAccess(
    "<root><data name=\"a&#x200D;b\"><value>One</value></data><data name=\"ab\"><value>Two</value></data></root>"
)]
internal static class UnicodeMemberFamilies { }

[DeterministicResxAccess(
    "<root><data name=\"__resxResourceManager\"><value>Text</value></data></root>"
)]
internal static class ResourceManagerMember { }

// Omitted Reference Resource entries retain structure and Placeholder Contract validation.
[DeterministicResxAccess("<root><data name=\"invalid-key\"><value>{name</value></data></root>")]
internal static class WarnMalformedPlaceholder { }

[DeterministicResxAccess(
    "<root><data name=\"invalid-key\"><value>{name</value></data></root>",
    InvalidKeyHandling = InvalidKeyHandling.Ignore
)]
internal static class IgnoreMalformedPlaceholder { }

[DeterministicResxAccess(
    "<root><data name=\"invalid-key\" type=\"System.Int32\"><value>7</value></data></root>"
)]
internal static class WarnNonText { }

[DeterministicResxAccess(
    "<root><data name=\"invalid-key\" type=\"System.Int32\"><value>7</value></data></root>",
    InvalidKeyHandling = InvalidKeyHandling.Ignore
)]
internal static class IgnoreNonText { }

// Omitted Localized Resource entries retain Placeholder Contract validation.
[DeterministicResxAccess(
    "<root><data name=\"invalid-key\"><value>{name@int}</value></data></root>",
    localizedXml: "<root><data name=\"invalid-key\"><value>{name@string}</value></data></root>"
)]
internal static class WarnLocalizedContract { }

[DeterministicResxAccess(
    "<root><data name=\"invalid-key\"><value>{name@int}</value></data></root>",
    localizedXml: "<root><data name=\"invalid-key\"><value>{name@string}</value></data></root>",
    InvalidKeyHandling = InvalidKeyHandling.Ignore
)]
internal static class IgnoreLocalizedContract { }

// Unsupported attribute inputs do not silently select an omission policy.
[DeterministicResxAccess(
    "<root><data name=\"invalid-key\"><value>Text</value></data></root>",
    InvalidKeyHandling = (InvalidKeyHandling)99
)]
internal static class UnsupportedIdentifierPolicy { }
