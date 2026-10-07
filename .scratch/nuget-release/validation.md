# First NuGet release validation

Validated on 2026-10-07 for the user-approved manual NuGet.org publication.
Revalidated the archive repacked after review on the same date, and refreshed its
checksum and the local PackageConsumer as approved by the user.

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
| Size | 43,624 bytes |
| Repository revision in manifest | `2819406c461099692bfb90c6eee78595e3cefddb` |
| Working branch | `feature/nuget-first-release` |

SHA-256 of the exact validated archive:

```text
3F5BFCFB6AF26A3E3AF49717F590A0213C55C15497C0E61AA88AA314AF133741
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
| Local PackageConsumer with the exact delivered archive | Passed: Raw Text and mixed named/indexed Formatted Text; three `FormatWelcome` overloads |

Final TRX files are retained under the ignored directory:

```text
test-results/execution/full-429f3258e0ca4ffb87ffb7f135290d70/Talby.Core.ResxAccess/
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

## Local PackageConsumer follow-up

The original `artifacts/PackageConsumer` referenced the local `1.0.0` archive built
on 2026-10-02, before Formatted Text generation was implemented. Its generated
`Program.cs` therefore contained only Raw Text overloads for `Welcome`. An isolated
reproduction with that archive confirmed zero `FormatWelcome` overloads.

The consumer now references `0.1.0-beta.1`. Its `NuGet.Config` selects
`artifacts/nuget` for this package and NuGet.org for dependencies, and its project
uses a separate package cache under `obj/packages`. The restored `.nupkg` hash was
checked against the delivered archive's hash above. For future repacks of the same
version, use a fresh consumer package cache before restoring again.

`MetalamaEmitCompilerTransformedFiles=true` refreshes the inspectable output at
`artifacts/PackageConsumer/obj/Release/net10.0/metalama/Program.cs`. The unchanged
Translation `Hello {name@string}, {0:N2}!` generates three `FormatWelcome` overloads,
with required parameters `string name` and `object? arg0`, followed by zero, one or
two culture parameters. `Welcome()` continues to return Raw Text by contract.

The local check was:

```powershell
dotnet run --project artifacts/PackageConsumer/PackageConsumer.csproj --configuration Release
```

Runtime output:

```text
Just text
Hello {name@string}, {0:N2}!
Hello Ada, 12.50!
```

The current archive passed the complete suite after a Release restore/build:
20 UnitTests, 12 AspectTests and 38 IntegrationTests. Its SHA-256 file was updated
only after these checks passed, with a check that the archive had not changed
during validation. No library implementation change was needed.

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
