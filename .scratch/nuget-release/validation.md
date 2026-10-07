# First NuGet release validation

Validated on 2026-10-07 for the user-approved manual NuGet.org publication.

## Delivered archive

| Field | Value |
| --- | --- |
| Package ID | `Talby.Core.ResxAccess` |
| Version | `0.1.0-beta.1` |
| Target Framework | `net10.0` |
| Authors | `TalbyAI` |
| Copyright | `Copyright (c) 2026 TalbyAI` |
| License | MIT expression, with included license text |
| Metalama dependency | `Metalama.Framework` `2026.1.28` |
| Archive | `artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg` |
| Size | 43,620 bytes |
| Repository revision in manifest | `852adfcc2bf8f511ad99c1b36ee3a6ac6220b875` |
| Working branch | `feature/nuget-first-release` |

SHA-256 of the exact validated archive:

```text
11A1344A1CA1320CFCA4ACF9CB7345C68F2D3FC4C2987C58BB3A2B3F4B7B324C
```

The same hash is stored in the adjacent `.nupkg.sha256` file. The archive includes
the MIT `LICENSE`, package `README.md`, `buildTransitive/Talby.Core.ResxAccess.targets`
and `lib/net10.0/Talby.Core.ResxAccess.dll`. The manifest carries the package metadata,
repository URL/revision and Metalama dependency.

## Verification evidence

Environment: .NET SDK 10.0.401, Node.js 24.14.1, npm 11.17.0, PowerShell 7.6.6.

| Verification | Result |
| --- | --- |
| `dotnet tool restore` | Passed |
| `npm ci` | Passed |
| `npm run format:check` | Passed |
| `dotnet restore Talby.Core.ResxAccess.slnx` | Passed |
| Release solution build with `--no-restore` | Passed; 0 errors, 2 expected `TRESX006` consumer fixture warnings |
| Normal full runner | Passed: 20 UnitTests, 12 AspectTests, 38 IntegrationTests |
| Pack with `--configuration Release --no-build --no-restore` | Passed |
| Final full runner with `TALBY_TEST_PACKAGE` set to the delivered archive | Passed: 70 tests, 0 failures, 0 skipped |

Final TRX files are retained under the ignored directory:

```text
test-results/execution/full-5bbb3f62f29e46cc93d60580e59f1642/Talby.Core.ResxAccess/
```

The final run used:

```powershell
$env:TALBY_TEST_PACKAGE = (Resolve-Path artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg).Path
pwsh -NoProfile -File tests/run.ps1 -Mode full
Remove-Item Env:TALBY_TEST_PACKAGE
```

The fixture copies the provided archive into its own local feed and starts with
a fresh package cache. Source mapping selects that feed for this package and
NuGet.org for its dependencies. The consumer project contains `PackageReference`
without `ProjectReference` or an explicit targets import.

Runtime assertions check Reference and Localized Raw Text, named typed formatting
arguments, independent Formatting Culture, Spanish parent fallback, Reference
Resource fallback and an actual satellite assembly. Existing tests and their
assertions were retained; two package Facts were added. Fast/full selection and
the solution's five-project structure are unchanged.

## Failure controls and review

- Before metadata changes, the archive test rejected the default Authors value
  `Talby.Core.ResxAccess` instead of the required `TalbyAI`.
- The first package consumer exposed a Windows cleanup issue: the compiler server
  retained an open Metalama assembly in the disposable consumer's package cache.
  An exclusive-open probe confirmed the file lock. The isolated cache now remains
  with ignored test artifacts, outside the disposable consumer. Runtime assertions
  pass after this correction.
- Independent review identified missing checks for a stale artifact's version,
  copyright and Metalama dependency version. The archive test now compares those
  fields with the library project's declarations.
- A copied archive with only its manifest version changed to `0.1.0-beta.0` was
  rejected with expected `0.1.0-beta.1` versus actual `0.1.0-beta.0`. The valid archive
  subsequently passed both package tests and the final full run.

## Publication prerequisites

Rechecked public NuGet profiles on 2026-10-07:

- [TalbyAI](https://www.nuget.org/profiles/TalbyAI) exists publicly.
- [Talby](https://www.nuget.org/profiles/Talby) returns Page Not Found.

Public profile data does not establish account type, the user's account linkage
or organization membership. Authenticated owner verification remains a prerequisite
for the user's upload. The public package ID was unlisted during preparation;
NuGet checks ID availability and prefix restrictions on upload.

Follow [Manual NuGet release](../../docs/releasing.md) to check the checksum, verify
the authorized TalbyAI owner, review metadata/README and submit the exact archive.
The report records local validation; it does not claim successful public publication.
