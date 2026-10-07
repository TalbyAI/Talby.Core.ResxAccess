# First NuGet release

Approved by the user on 2026-10-07 after the `grilling` interview.

## Release contract

- Publish target: NuGet.org, with manual publication performed by the user.
- Package ID: `Talby.Core.ResxAccess`.
- First version: `0.1.0-beta.1`, for existing projects and early adopters.
- Target Framework: `net10.0`; preserve Metalama.Framework `2026.1.28`.
- License: MIT, with Authors and copyright attribution to TalbyAI.
- Include package metadata, a package README, the license and transitive targets.
- Deliver a locally validated `.nupkg` and English manual release instructions.
- Verify the complete existing suite and a real consumer using only `PackageReference`
  for the library, without an explicit targets import. Check generated Raw Text,
  Formatted Text, Resource Culture fallback and satellite resource loading.
- Retain existing test assertions, project settings and fast/full selection.

## Ownership prerequisite

On 2026-10-07, the public `TalbyAI` NuGet profile returned HTTP 200, while `Talby`
returned HTTP 404. The public profile does not identify account type or prove the
publishing user's access. The release guide must require authenticated verification
of the selected owner before publication. Authors metadata does not set ownership.

The preferred first-publish route is the NuGet.org Upload page: select the authorized
owner, inspect the metadata and README, then submit the exact validated artifact.
Account creation, membership changes, API key creation and publication are performed
by the user as needed, outside this preparation.

## Acceptance

Formatting, Release restore/build and `tests/run.ps1 -Mode full` pass. Package tests
inspect the actual archive and compile/run an isolated local-feed consumer. The
delivered package has a recorded SHA-256 checksum and clear publication instructions.
