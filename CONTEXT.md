# Resource Access

This context provides access to translated text and checks the consistency of resource entries across cultures.

## Language

**Resource Set**:
A Reference Resource and the Localized Resources containing its translations.

**Reference Resource**:
The culture-neutral resource that defines the complete set of Resource Keys.
_Avoid_: Master resource, default language file

**Localized Resource**:
A resource containing the translations of a Resource Set for a particular culture.

**Expected Culture**:
A culture for which a Resource Set must include a Localized Resource, even if no corresponding resource has been discovered.

**Resource Key**:
The identifier of a resource entry across its translations.
_Avoid_: Translation ID, label

**Translation**:
The text associated with a Resource Key for a particular culture.

**Raw Text**:
A Translation whose formatting placeholders have not been substituted.
_Avoid_: Untranslated text

**Formatted Text**:
A Translation whose formatting placeholders have been substituted with supplied arguments.

**Formatting Placeholder**:
A position in a Translation at which a supplied argument is inserted.

**Indexed Placeholder**:
A Formatting Placeholder that identifies its argument by a numeric index.

**Named Placeholder**:
A Formatting Placeholder that identifies its argument by name and can declare an argument type and format.

**Argument Type**:
The type of value accepted for a Formatting Placeholder, including whether that value may be null.

**Argument Format**:
The presentation specification applied to a Formatting Placeholder's argument.

**Placeholder Contract**:
The exact set of argument identities and types established by a Reference Resource's Translation. Every corresponding Translation must use all and only those argument identities, though occurrence order and repetition may differ.

**Resource Culture**:
The culture requested when selecting a Translation.

**Formatting Culture**:
The culture used when formatting arguments inserted into a Translation.
