#if TEST_OPTIONS
// @Include(_DeterministicResxAccessAttribute.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResxAccessImplementation.cs)
// @Include(../../src/Talby.Core.ResxAccess/ReferenceResourceReader.cs)
// @Include(../../src/Talby.Core.ResxAccess/PlaceholderContract.cs)
// @Include(../../src/Talby.Core.ResxAccess/ResourceValidationException.cs)
#endif
namespace Consumer.Api;

// Reference Resource Named Placeholder declarations and parameter collisions.
[DeterministicResxAccess("<root><data name=\"Bad\"><value>{bad-name}</value></data></root>")]
internal static class InvalidIdentifier { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{name@float}</value></data></root>")]
internal static class UnsupportedType { }

[DeterministicResxAccess(
    "<root><data name=\"Bad\"><value>{name@System.Int32}</value></data></root>"
)]
internal static class UnsupportedTypeSpelling { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{name@int??}</value></data></root>")]
internal static class MalformedNullableType { }

[DeterministicResxAccess(
    "<root><data name=\"Bad\"><value>{name@int} {name@long}</value></data></root>"
)]
internal static class ConflictingTypes { }

[DeterministicResxAccess(
    "<root><data name=\"Bad\"><value>{name@string} {name@string?}</value></data></root>"
)]
internal static class ConflictingNullability { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{arg2} {02}</value></data></root>")]
internal static class IndexedNameCollision { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{resourceCulture}</value></data></root>")]
internal static class ResourceCultureCollision { }

[DeterministicResxAccess(
    "<root><data name=\"Bad\"><value>{formattingCulture@string?}</value></data></root>"
)]
internal static class FormattingCultureCollision { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{name,}</value></data></root>")]
internal static class MalformedAlignment { }

[DeterministicResxAccess("<root><data name=\"Bad\"><value>{name:{}}</value></data></root>")]
internal static class MalformedFormat { }

// Localized Resource Named and mixed Placeholder Contracts.
[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name@int}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name@long}</value></data></root>"
)]
internal static class ChangedType { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name@string?}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name@string}</value></data></root>"
)]
internal static class ChangedReferenceNullability { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name@int?}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name@int}</value></data></root>"
)]
internal static class ChangedValueNullability { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name@string}</value></data></root>"
)]
internal static class ChangedUntypedArgument { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name@string?}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value /></data></root>"
)]
internal static class MissingNullableIdentity { }

[DeterministicResxAccess(
    "<root><data name=\"omitted-key\"><value>{name} {2} {0}</value></data></root>",
    localizedXml: "<root><data name=\"omitted-key\"><value>{name} {0} {1} {2}</value></data></root>"
)]
internal static class AdditionalIndexedIdentity { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name} {2} {0}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{0} {2}</value></data></root>"
)]
internal static class MissingNamedIdentity { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name} {extra}</value></data></root>"
)]
internal static class AdditionalNamedIdentity { }

[DeterministicResxAccess(
    "<root><data name=\"Summary\"><value>{name}</value></data></root>",
    localizedXml: "<root><data name=\"Summary\"><value>{name</value></data></root>"
)]
internal static class MalformedNamedTranslation { }
