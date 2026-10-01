# Repository Guidelines

## Documentation & Domain Language

- Always write repository documents in English, regardless of the language used in conversations with the user.
- Use English for all domain terms in documentation, code identifiers, and conversations, even when the surrounding conversation uses another language.

## Project Structure & Module Organization

- `Talby.Core.ResxAccess.slnx` groups the library and test projects.
- `src/Talby.Core.ResxAccess/` contains the .NET 10 library, which references `Metalama.Framework`. It currently has no implemented library functionality.
- `tests/Talby.Core.ResxAccess.Tests/` contains xUnit tests and references the library. `MetalamaSetupTests.cs` demonstrates creating and querying a Metalama compilation.
- `global.json` selects the SDK; `README.md` documents setup in Spanish. No resource assets are currently checked in.

## Build, Test, and Development Commands

Install .NET SDK 10.0.401 or a later patch in the 10.0.4xx feature band, as allowed by `global.json`. NuGet.org access is required for package restore. Run from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

These commands restore dependencies, compile both projects, and execute tests in sequence. Rebuild after code changes before using `--no-build`. This repository contains a library, so there is no application to launch locally.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, file-scoped namespaces, and braces on separate lines. Use PascalCase for types and methods, camelCase for parameters and local variables, and descriptive filenames matching their primary types. Both projects enable nullable reference types and implicit usings; preserve these settings. Project XML uses two-space indentation. No repository-specific formatter or lint configuration is present.

## Testing Guidelines

Use xUnit `[Fact]` tests with descriptive PascalCase method names, such as `CanCreateAndQueryCompilation`, in `*Tests.cs` files. For Metalama code-model tests, follow the existing `UnitTestClass` and disposable `CreateTestContext()` pattern. Add focused tests for new behavior and bug fixes. No coverage threshold is configured.

Keep `MetalamaEnabled=false` in the test project and `MetalamaRemoveCompileTimeOnlyCode=false` in the library; these settings support testing compile-time helpers.

## Commit & Pull Request Guidelines

The initial commit uses `chore: initialize .NET solution with Metalama and unit tests`. Follow this concise, type-prefixed style, choosing an appropriate prefix such as `feat:`, `fix:`, or `docs:`. Keep changes focused.

Pull requests should describe the change, link related issues when applicable, and report build/test results. Update setup documentation when prerequisites or commands change. Exclude generated `bin/`, `obj/`, and test-result files.

## Agent skills

### Issue tracker

Issues and specs use local Markdown under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, and `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: root `CONTEXT.md` and `docs/adr/`. See `docs/agents/domain.md`.
