# Issue tracker: Local Markdown

Issues and specs for this repo live as Markdown files in `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`
- The spec is `.scratch/<feature-slug>/spec.md`.
- Implementation issues are one file per ticket at
  `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`.
  Never use a single combined tickets file.
- Triage state is a `Status:` line near the top of each issue file.
  See `triage-labels.md` for the role strings.
- Append comments and conversation history under a `## Comments` heading.

## When a skill says "publish to the issue tracker"

Create a new file under `.scratch/<feature-slug>/`, creating the directory
if needed.

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path. If only an issue number is supplied,
resolve it within the relevant feature directory; ask for the feature if
the number is ambiguous.

## Wayfinding operations

Used by `/wayfinder`. The map is a file with one child file per ticket.

- Map: `.scratch/<effort>/map.md`, containing Notes, Decisions-so-far,
  and Fog.
- Child ticket: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`,
  with the question in the body. A `Type:` line records
  `research`, `prototype`, `grilling`, or `task`.
- Blocking: a `Blocked by: NN, NN` line near the top. A ticket is unblocked
  when every ticket it lists is resolved.
- Frontier: scan for open, unblocked, unclaimed tickets; first by number wins.
- Claim: set `Status: claimed` and save before work.
- Resolve: append the answer under `## Answer`, set `Status: resolved`,
  and append a gist and ticket link to Decisions-so-far in the map.

Wayfinding uses `claimed` and `resolved` as workflow states; ordinary
issue triage uses the roles in `triage-labels.md`.
