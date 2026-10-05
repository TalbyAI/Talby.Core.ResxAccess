# Repository Guidelines

## Documentation & Domain Language

- Always write repository documents in English, regardless of the language used in conversations with the user.
- Use English for all domain terms in documentation, code identifiers, and conversations, even when the surrounding conversation uses another language.

## Project Structure & Module Organization

- `Talby.Core.ResxAccess.slnx` groups the library and test projects.
- `src/Talby.Core.ResxAccess/` contains the .NET 10 library and references `Metalama.Framework`.
- `tests/Talby.Core.ResxAccess.UnitTests/` contains `MetalamaSetupTests` and `ReferenceResourceTests`, including tests of internal compile-time helpers.
- `tests/Talby.Core.ResxAccess.IntegrationTests/` contains `RawTextConsumerTests`, `LocalizedResourceConsumerTests`, and the `ConsumerProject` helper, and references the precompiled ConsumerFixture.
- `tests/Talby.Core.ResxAccess.AspectTests/` contains the dedicated Metalama snapshot tests; `tests/Talby.Core.ResxAccess.ConsumerFixture/` contains the real SDK consumer executable and its resource assets.
- `global.json` selects the SDK; `README.md` documents setup and test execution in English.

## Build, Test, and Development Commands

Install .NET SDK 10.0.401 or a later patch in the 10.0.4xx feature band, as allowed by `global.json`. NuGet.org access is required for package restore. Run from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

These commands restore dependencies and compile all five solution projects, then execute all three test projects. Rebuild after code changes before using `--no-build`. This repository contains a library, so there is no application to launch locally.

For independent UnitTests or IntegrationTests execution after the solution build:

```powershell
dotnet test tests/Talby.Core.ResxAccess.UnitTests/Talby.Core.ResxAccess.UnitTests.csproj --configuration Release --no-build --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore
```

The PowerShell 7 runner `pwsh -NoProfile -File tests/run.ps1 -Mode fast` selects UnitTests and AspectTests, deferring IntegrationTests. Use `-Mode full` before merge to execute all 43 tests and validate the inventory. Update `tests/run.ps1` when adding, grouping, or renaming tests.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, file-scoped namespaces, and braces on separate lines. Use PascalCase for types and methods, camelCase for parameters and local variables, and descriptive filenames matching their primary types. All projects enable nullable reference types and implicit usings; preserve these settings. Project XML uses two-space indentation. No repository-specific formatter or lint configuration is present.

## Testing Guidelines

Before adding tests, grouping scenarios, or optimizing test execution, read [docs/agents/testing.md](docs/agents/testing.md) for test placement, assertion preservation, negative controls, and performance measurements.

Use xUnit `[Fact]` tests with descriptive PascalCase method names, such as `CanCreateAndQueryCompilation`, in `*Tests.cs` files. For Metalama code-model tests, follow the existing `UnitTestClass` and disposable `CreateTestContext()` pattern. Add focused tests for new behavior and bug fixes. No coverage threshold is configured.

Keep `MetalamaEnabled=false` in UnitTests and IntegrationTests and `MetalamaRemoveCompileTimeOnlyCode=false` in the library; these settings support testing compile-time helpers. Keep the Metalama UnitTesting dependency and library internals access in UnitTests; keep the ConsumerFixture reference in IntegrationTests.

## Commit & Pull Request Guidelines

- Once `origin` is configured, never create a commit directly on `main`; commit on a working branch.
- If a commit is needed while the current branch is `main`, preserve pending changes, run `git fetch origin`, and update `main` to match `origin/main` before creating and switching to a new working branch. Restore pending changes on that branch and commit there.
- Use a fast-forward update (`git merge --ff-only origin/main`) and verify that `main` and `origin/main` point to the same commit. If they differ or synchronization fails, stop without discarding changes or local commits.
- Temporary exception: while no `origin` remote is configured, these branch restrictions are suspended and commits directly on `main` are allowed.

The initial commit uses `chore: initialize .NET solution with Metalama and unit tests`. Follow this concise, type-prefixed style, choosing an appropriate prefix such as `feat:`, `fix:`, or `docs:`. Keep changes focused.

Pull requests should describe the change, link related issues when applicable, and report build/test results. Update setup documentation when prerequisites or commands change. Exclude generated `bin/`, `obj/`, and test-result files.

## Agent skills

### Issue tracker

Issues and specs use local Markdown under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.
