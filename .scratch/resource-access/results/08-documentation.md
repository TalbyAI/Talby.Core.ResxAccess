# Resource Access documentation verification

Issue: [08 - Document Resource Access](../issues/08-document-resource-access.md)

## Delivered documentation

The [consumer guide](../../../docs/resource-access.md) provides a complete SDK
project setup and runnable consumer files. The [README](../../../README.md) links
to the guide, retains repository setup and solution-level checks including
AspectTests, and records the current test inventory. No library API, prerequisite,
build target, IDE configuration or existing test assertion changed.

| Issue requirement | Guide coverage |
| --- | --- |
| Attribute and target shape | Adopt the API: non-generic static class without `partial`, project-relative Reference Resource, optional Expected Cultures and identifier policy. |
| Complete Resource Set validation | Resource Set validation and discovery: same-directory association, case-sensitive keys, text-only values, all discovered cultures and omitted keys, fallback distinction. |
| Raw Text and formatting overloads | Generated methods and cultures: two Raw Text overloads, three formatting overloads, independent cultures, runtime failures. |
| Placeholder syntax and types | Formatting Placeholders and Translation compatibility: all supported types and nullable forms, required arguments, alignment, formats, escaped braces and contracts. |
| Mixed ordering | Runnable mixed example and named-first/numeric-indexed ordering with index gaps; single-style recommendation and ADR link. |
| Identifiers and diagnostics | Resource Key identifiers and diagnostics: policies, keywords, deterministic suffixes, parameter/member conflicts and diagnostic IDs. |
| Build and IDE integration | Embedding and incremental builds plus IDE evidence and deferred requirements: supported SDK metadata, custom configuration errors, reproducible checks and recorded limitations. |
| Executable examples and links | Exact guide snippets compiled/invoked by the new DocumentationConsumerTests; local links and heading anchors checked. |
| English setup documentation | Guide and README use the glossary and retain .NET SDK, Node.js, formatter and full solution checks. Both ADRs remain unchanged. |

## Consumer verification

`DocumentationConsumerTests.CompilesAndInvokesConsumerGuideExamples` reads the
guide's `Texts.cs`, `Program.cs`, Reference Resource and Spanish Localized Resource
code blocks. It uses the existing `ConsumerProject` SDK boundary, with standard
embedding and the source targets import, then invokes the consumer in a fresh
process. The fixture's existing repository-path helper is shared internally to
locate the guide; no new consumer infrastructure or production behavior is added.

Fifteen literal output assertions verify both Raw Text overloads, all three Named
formatting overloads, independent cultures, parent and Reference Resource fallback,
unchanged placeholders, Indexed gaps, mixed ordering with reordered Translations,
alignment, escaped braces, required nullable arguments, normalization and keyword
escaping. The test first failed because the guide was absent, then passed after
the exact documented examples were added.

All previous 67 tests and their assertions remain unchanged. One new IntegrationTest
extends the inventory to 20 UnitTests, 12 AspectTests and 36 IntegrationTests:
32 fast / 68 full. Tests were neither grouped nor renamed; existing compilation
isolation and failure granularity remain unchanged. This is documentation
verification, with no test-execution performance claim.

## Validation and reproduction

Verification on Windows on 2026-10-05 used .NET SDK 10.0.401, Metalama 2026.1.28,
Node.js 24.14.1 and npm 11.17.0. Solution restore, Release build, the focused
documentation test and formatting checks passed. The full runner passed and
validated all 68 results. The two existing ConsumerFixture `TRESX006` warnings
remain intentional. Full-run TRX files are retained under ignored
`test-results/execution/full-81f2af53b52544afa0a6a7532a8c9243/`.

Reproduce from the repository root with the prerequisites in the README:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~DocumentationConsumerTests
npm run format:check
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

Local documentation links and heading anchors in the guide, README, issue, map and
this report were checked against repository files. IDE claims are limited to the
[existing evidence](07-ide-refresh.md): successful automatic refresh and non-partial
IDE support remain deferred. No additional IDE verification is claimed.

## Code review

Both independent axes reviewed the staged changes against task-start commit
`a6e4bb6587c31e6ae818261c082f911c729c309d` before committing.

### Standards

No findings. The review checked glossary usage, both ADRs, consumer test placement,
assertion preservation, tracker conventions and recorded verification. No
documented-standard breach or relevant baseline code smell was identified.

### Spec

No findings. The review checked all issue 08 criteria and compared API, embedding,
incremental build and IDE claims with the implementation and existing evidence.
The guide preserves issue 07's explicit deferral without claiming automatic
refresh or non-partial IDE support. Total findings: Standards 0; Spec 0.
