# Generate Indexed Placeholder formatting methods

Status: ready-for-agent
Type: AFK
User stories covered: 17-18, 21, 24-28, 31-32, 35, 45-46, 48

## Parent

[Resource Access Specification](../spec.md)

## What to build

A consumer supplies arguments to generated `Format` methods for Resource Keys containing Indexed Placeholders and receives Formatted Text from the selected Translation. The Reference Resource establishes numeric argument identities; Translations can reorder or repeat them and change their presentation without changing the call signature. Resource Culture and Formatting Culture remain independent.

## Acceptance criteria

- [ ] Resource Keys with Formatting Placeholders receive `Format`-prefixed public static methods with three overloads: arguments alone, arguments followed by `resourceCulture`, and arguments followed by `resourceCulture` and `formattingCulture`. Keys without placeholders retain only Raw Text methods; no formatting-culture-only overload is introduced.
- [ ] Indexed arguments use required `object?` parameters named by their identities and ordered numerically. Gaps are allowed: indices 0 and 2 produce `arg0` and `arg2`, with no parameter for index 1.
- [ ] Every Translation uses all and only the Reference Resource's argument identities. Reordering, repetition, and different Argument Formats succeed; additional or omitted identities produce compilation errors across the entire Resource Set.
- [ ] Support standard composite-format alignment, Argument Formats, escaped braces, and null arguments. Raw Text remains unchanged; invoked formatting methods return expected Formatted Text.
- [ ] Malformed Formatting Placeholders produce compilation errors. Empty or whitespace-only text remains valid only when its Placeholder Contract permits it.
- [ ] Formatting Culture defaults to `CurrentCulture` independently of Resource Culture, including when Resource Culture is explicit. Verify all overloads using distinct ambient and explicit cultures, selected Translations, and resource fallback; restore ambient cultures afterward.
- [ ] Standard formatting failures propagate their corresponding exceptions. For example, a valid placeholder syntax with an incompatible Argument Format fails at runtime rather than being wrapped in an unrelated exception.
- [ ] Add generated-signature and diagnostic aspect snapshots plus actual consumer compilation and invocation checks. Assertions observe API, diagnostics, and returned text rather than parser internals or generated helper names.

## Blocked by

- [02 - Discover and validate complete Localized Resources](02-validate-localized-resources.md)
