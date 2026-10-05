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

The PowerShell 7 runner `pwsh -NoProfile -File tests/run.ps1 -Mode fast` selects the test projects listed in `$fastProjects` (currently UnitTests and AspectTests), deferring other test projects. Use `-Mode full` before merge to execute every test project in the solution. The runner validates TRX results without maintaining test names or fixed counts. Adding or renaming tests needs no runner change; update `$fastProjects` only when changing fast project selection, and add new projects to the solution for full execution.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, file-scoped namespaces, and braces on separate lines. Use PascalCase for types and methods, camelCase for parameters and local variables, and descriptive filenames matching their primary types. All projects enable nullable reference types and implicit usings; preserve these settings. Project XML uses two-space indentation.

CSharpier is pinned in `.config/dotnet-tools.json`; markdownlint-cli2, Husky and lint-staged are pinned in `package.json` and `package-lock.json`. Install Node.js 24, run `dotnet tool restore` and `npm ci`, then use `npm run format` to fix formatting and `npm run format:check` to verify it. Resolve any remaining Markdown errors manually. Only Markdown line length (`MD013`) is disabled, to allow long tables and commands.

The pre-commit hook formats staged files and rejects remaining errors. Keep partial staging intact; do not stage unrelated changes. The CLI commands and hook share the formatter exclusions. Metalama output snapshots (`*.t.cs`, `*.i.cs`), `.resx`, dependencies and generated output are excluded. Future prototypes must live under the top-level `prototypes/` directory, which is excluded from both formatters, the hook and CI formatting checks. Repository documents, including `.scratch/`, remain in scope.

GitHub Actions runs formatting checks, solution restore, Release build and `tests/run.ps1 -Mode full` for pull requests and pushes to `main`, and retains TRX results. CI checks formatting without modifying files. Branch protection must require the validation check separately if merge blocking is needed.

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
