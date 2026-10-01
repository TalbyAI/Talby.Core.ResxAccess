# Resource Access Design

This document records the decisions agreed during the design interview. The interview is ready for final shared-understanding confirmation; implementation has not started.

## Confirmed requirements

- Use Metalama to generate a static resource access API on the class annotated with `GenerateResxAccessAttribute`.
- The target is a consumer-declared, non-generic static class. Generated methods are public; the consumer selects the class name and namespace.
- The attribute identifies a culture-neutral reference `.resx` file.
- Resolve the reference path relative to the consumer project directory. Discover `<reference-name>.<culture>.resx` only in the same directory as the reference resource.
- The initial version supports embedded resources under the .NET SDK's standard conventions and must resolve their effective manifest base names. Custom embedding and naming configurations, including explicit `LogicalName` or `ManifestResourceName` and linked resources, are outside the initial scope and receive explicit diagnostics.
- Discover and validate all associated localized resources. Optional `ExpectedCultures` additionally requires a localized resource for every listed culture; it does not exclude other discovered cultures from validation.
- Every associated localized resource must have exactly the same case-sensitive resource keys as the reference resource. Missing and additional keys are compilation errors.
- Support text resources only in the initial version.
- Provide access to raw translated text and parameterized formatted text.
- Support standard .NET indexed placeholders and named placeholders with argument formats and a selection of built-in argument types.
- Named placeholder syntax is `{name[@type][,alignment][:format]}`, with `{{` and `}}` as escaped braces. Initially support `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, and `Guid`. Untyped arguments, including indexed arguments, have type `object?`.
- Support a nullable `?` suffix on declared argument types, including value types: `{amount@decimal?:N2}` declares `decimal?`. Explicit types preserve C# nullability: `@string` declares `string`, and `@string?` declares `string?`. Non-nullable reference types rely on compiler annotations without additional runtime null checks.
- The reference resource defines placeholder identities and argument types. Each translation must use the same arguments; reordering, repetition, and different formats are allowed. Translations may omit types, but explicit types must match the reference.
- Indexed arguments retain their numeric identities independently of named arguments. Index gaps are allowed; only used arguments become parameters. For `{name} {2} {0}`, the ordered parameters are `name`, `arg0`, and `arg2`. A translation introducing index `1`, omitting `name`, `0`, or `2`, or adding another named argument is invalid.
- Named and indexed placeholders may coexist in one resource key. Named parameters appear first, in order of first appearance in the reference translation; indexed parameters follow in numeric order. Documentation should discourage mixing styles without rejecting it.
- Use `CurrentUICulture` by default for resource selection and provide overloads accepting an explicit culture.
- Use `CurrentCulture` by default for argument formatting, independently of the resource culture. Allow a separate explicit formatting culture.
- Report resource validation failures as compilation errors.
- `InvalidKeyHandling` supports `Warn` (default), `Ignore`, and `Normalize`. Warn and Ignore omit generated members for invalid keys, with or without a warning respectively; resource validation still applies. Normalize replaces invalid identifier characters with `_`, preserves valid identifiers, and adds suffixes when needed to resolve normalization collisions. C# keywords use identifier escaping.
- Reserve valid resource key identifiers before assigning normalized identifiers. Process normalized keys in ordinal order and append `_2`, `_3`, and subsequent suffixes as needed for reproducible collision resolution.
- Named argument identifiers must be valid C# identifiers and must not collide with another generated parameter, including indexed argument parameters and culture parameters. Violations are compilation errors; declared argument names are not silently renamed.
- Raw access uses overloaded methods named after each representable resource key: `Welcome()` and `Welcome(CultureInfo resourceCulture)`. Only resources with placeholders additionally receive `FormatWelcome(arguments...)`, plus overloads ending in `resourceCulture` and optionally `formattingCulture`. Collisions with existing members or between generated member families are compilation errors.
- Runtime lookup uses standard .NET resource fallback, including parent cultures and finally the reference resource.
- Duplicate keys, non-text values, and malformed placeholders are compilation errors. Empty and whitespace-only text is permitted, provided it satisfies the placeholder contract.
- Formatting failures propagate the corresponding standard exceptions. Runtime resource failures produce descriptive exceptions.
- Changes to `.resx` files alone must update generation and validation during incremental builds and in the IDE without manual recompilation. This is an acceptance requirement whose technical integration remains unverified.

## Attribute usage

```csharp
[GenerateResxAccess(
    "Resources/Messages.resx",
    ExpectedCultures = new[] { "es", "en" },
    InvalidKeyHandling = InvalidKeyHandling.Warn)]
public static class Messages
{
}
```

`ExpectedCultures` and `InvalidKeyHandling` are optional. The reference resource is required and must be culture-neutral. A `partial` declaration is not required.

## Generated API example

For a `Welcome` resource with reference text `Hello {name@string}`:

```csharp
Messages.Welcome();
Messages.Welcome(resourceCulture);

Messages.FormatWelcome("Ada");
Messages.FormatWelcome("Ada", resourceCulture);
Messages.FormatWelcome("Ada", resourceCulture, formattingCulture);
```

Raw access preserves the selected resource text without argument substitution. Formatted access inserts the arguments using the selected formatting culture. Parameter order and types always come from the reference resource, independently of the selected resource culture. A resource without placeholders receives only the raw access overloads.

## Nullable type semantics

Preserve C# type spelling: `@string` declares `string`, `@string?` declares `string?`, `@decimal` declares `decimal`, and `@decimal?` declares `decimal?`. Untyped placeholders retain `object?`. Nullability is part of the placeholder contract, so a translation that repeats an explicit type must match its reference nullability as well. A nullable argument remains a required parameter and a required placeholder identity; it does not allow translations to omit it. Null arguments retain standard .NET composite formatting behavior.

For example, `Total: {amount@decimal?:N2}` accepts a `decimal?` argument, and `Date: {date@DateTime?:yyyy-MM-dd}` accepts a `DateTime?` argument.

## Acceptance scenarios

- An attributed static class exposes raw access and correctly typed formatting overloads for a valid resource set, including nullable arguments and explicit cultures.
- All discovered cultures are validated; an absent expected culture, a missing or additional key, duplicate keys, non-text values, and malformed placeholders produce compilation errors.
- A translation may reorder or repeat arguments and change formats, but adding, omitting, or changing an argument's explicit type or nullability produces a compilation error.
- Mixed placeholders with index gaps preserve their identities: `{name} {2} {0}` generates only `name`, `arg0`, and `arg2`; no translation may introduce index `1` or omit a reference argument.
- Invalid keys follow Warn, Ignore, or Normalize while remaining subject to resource validation. Normalized names are reproducible, and member or parameter collisions produce diagnostics under the agreed rules.
- Runtime lookup respects resource fallback and independent resource and formatting cultures.
- Changes to resource contents and resource file additions or removals refresh both the build and the IDE without C# edits.

## Required technical verification

- Verify aspect application and generated overloads on a non-generic static target class.
- Verify resource manifest base name resolution and satellite resource lookup for the supported SDK configuration.
- Verify generation and diagnostics after modifying, adding, and removing a resource file without editing C# sources, in both incremental builds and the IDE.
- Verify nullable value and reference parameter types in Metalama's introduced methods.

## Technical evidence

Metalama supports programmatic member introduction with dynamically constructed parameter lists and static introduction scope. Its examples include non-partial target classes; a partial declaration is not inherently required. See [Introducing members](https://doc.metalama.net/conceptual/aspects/advising/introducing-members) and [IntroductionScope](https://doc.metalama.net/api/metalama-framework-aspects-introductionscope).

Standard .NET composite formatting accepts indexed placeholders, so named placeholders require an extension that preserves the standard format semantics. See [Composite formatting](https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting).

Design-time regeneration after external resource changes is not yet verified. The reviewed Metalama project and compilation API documentation did not establish an external-file dependency tracking mechanism; this is an open verification item, not evidence that Metalama cannot support it. See [IProject](https://doc.metalama.net/api/metalama-framework-project-iproject) and [ICompilation](https://doc.metalama.net/api/metalama-framework-code-icompilation).

The SDK's resource manifest name can depend on the resource path, root namespace, a dependent C# type, and explicit resource metadata. Runtime lookup cannot safely assume that the target class name is the manifest base name. See [Resource manifest file names](https://learn.microsoft.com/en-us/dotnet/core/resources/manifest-file-names).

C# represents nullable value types as `Nullable<T>` and nullable reference types with compiler annotations; reference annotations alone do not enforce runtime checks. See [Nullable value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types) and [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types).

See [the glossary](../CONTEXT.md), [the strict validation decision](adr/0001-require-complete-localized-resources.md), and [the mixed placeholder parameter order decision](adr/0002-order-mixed-placeholder-parameters.md).
