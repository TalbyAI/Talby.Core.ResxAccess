# Domain Docs

This repository uses a single-context layout.

## Before exploring, read these

- Root `CONTEXT.md` for domain vocabulary.
- Relevant decisions in `docs/adr/`.

If these files do not exist, proceed silently. Do not flag their absence
or suggest creating them upfront. The `/domain-modeling` skill creates
them when terms or decisions are resolved.

## File structure

- `CONTEXT.md`: the Resource Access context and its glossary.
- `docs/adr/`: architectural decisions for this context.
- `src/`: library source.
- `tests/`: library tests.

## Use the glossary's vocabulary

Use the terms defined in `CONTEXT.md` when naming domain concepts in
issues, proposals, hypotheses, code, and tests. Avoid synonyms the
glossary explicitly discourages.

If a concept is missing, reconsider whether it belongs to the domain.
Note real vocabulary gaps for `/domain-modeling`.

## Flag ADR conflicts

Explicitly identify any proposal that contradicts an existing ADR,
including the decision and the reason to reconsider it.
