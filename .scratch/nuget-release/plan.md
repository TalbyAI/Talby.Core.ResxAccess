# First NuGet Release Implementation Plan

> Execute the user-approved preparation inline, using the existing repository checkout.

**Goal:** Deliver `Talby.Core.ResxAccess.0.1.0-beta.1.nupkg` with evidence and manual publication instructions.

**Architecture:** Keep the library and its Metalama integration intact. Add package metadata and documents, then exercise the SDK package boundary through IntegrationTests and validate the final archive in an isolated consumer.

**Tech Stack:** .NET SDK 10.0.401, Metalama.Framework 2026.1.28, xUnit, PowerShell 7, NuGet.org.

## Global constraints

- Package ID `Talby.Core.ResxAccess`, version `0.1.0-beta.1`, Target Framework `net10.0`.
- MIT license; TalbyAI Authors and copyright attribution.
- User performs manual publication; preparation performs local validation.
- English repository documents and existing fast/full test selection.
- Keep UnitTests/IntegrationTests `MetalamaEnabled=false` and library `MetalamaRemoveCompileTimeOnlyCode=false`.

## Task 1: Package validation boundary

Files: `tests/Talby.Core.ResxAccess.IntegrationTests/NuGetPackageConsumerTests.cs`, `ConsumerProject.cs`.

- [x] Add a shared fixture that packs the existing Release outputs into an isolated local feed.
- [x] Check the actual manifest, README, MIT license, library assembly and `buildTransitive` target.
- [x] Compile and run a consumer with `PackageReference`, a fresh package cache and local source mapping; assert Raw Text, Formatted Text, satellite loading and fallback.
- [x] Run the focused tests before package metadata changes. The metadata assertion failed on the missing TalbyAI Authors attribution.

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~NuGetPackageConsumerTests
```

## Task 2: Release metadata and documents

Files: library `.csproj`, `LICENSE`, library `README.md`, root `README.md`, `docs/resource-access.md`, `docs/releasing.md`.

- [x] Set PackageId, Version, Authors, Description, PackageLicenseExpression, PackageReadmeFile and repository/project URLs.
- [x] Pack the MIT license and consumer-focused README alongside the existing transitive targets.
- [x] Document first-publish owner verification, exact artifact selection, metadata review, upload and post-publication installation.
- [x] Update the current integration-test inventory and package adoption instructions.
- [x] Rebuild and run the focused package tests; both pass.

## Task 3: Deliver verified artifacts

Files: ignored `artifacts/nuget/`, `.scratch/nuget-release/validation.md`.

- [x] Run `npm run format:check`, solution Release restore/build and `pwsh -NoProfile -File tests/run.ps1 -Mode full`.
- [x] Pack into `artifacts/nuget`, validate that exact package in the same package-consumer boundary, and record its SHA-256 checksum.
- [x] Review the complete diff, record commands/results and prepare links to the artifact and release guide.

```powershell
dotnet pack src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj --configuration Release --no-build --no-restore --output artifacts/nuget
Get-FileHash artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg -Algorithm SHA256
```
