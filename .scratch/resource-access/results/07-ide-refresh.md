# IDE Resource Access refresh

Issue [07](../issues/07-refresh-ide-resource-access.md) remains `ready-for-human`.
Automatic VS Code refresh is not established. The successful SDK checks below
do not substitute for the required editor observations or human review.

## Environment and consumer

Verification on 2026-10-05 used Windows, .NET SDK 10.0.401, Metalama 2026.1.28,
VS Code 1.140.0 (`07f806f999227108933c2e30515b26eecc1fda74`), C# 11.1.32,
C# Dev Kit 11.1.3 and .NET Install Tool 3.2.0. The C# language server reports
Roslyn `5.12.0-1.26475.2`.

The isolated SDK consumer references the source library and explicitly imports
its transitive targets. It targets `net10.0`, enables nullable reference types
and implicit usings, and declares:

```csharp
[GenerateResxAccess("Resources/Texts.resx", ExpectedCultures = new[] { "es" })]
internal static partial class Texts { }
```

The Reference Resource starts with Resource Key `Welcome` and Translation
`Hello {name@string}`. The `es` Localized Resource contains `Hola {name}`.
`Program.cs` invokes `Texts.Welcome()` and `Texts.FormatWelcome("Ada")`.
The initial Debug build succeeds before opening the IDE. Both analyzer and
compiler diagnostics scopes are `fullSolution`, as recommended by
[Metalama's VS Code configuration](https://doc.metalama.net/conceptual/using/ide/vs-code).

## Implementation and automated coverage

The resource map now depends on `PrepareResourceNames`, including SDK target
paths, culture classification and manifest names. Calling only
`CreateManifestResourceNames` in a design-time build removed the unclassified
resources and caused an erroneous `TRESX003` in the live IDE.

Resource paths are registered as content-sensitive `AdditionalDesignTimeBuildInput`
items. A design-time-only generated C# declaration contains content hashes of
resources and the map; the map includes discovery membership. The aspect reads
this type through Metalama's code model before validation to register a dependency.
Unchanged inputs preserve the generated file's contents and timestamp.
Ordinary builds retain the issue 06 `AdditionalFiles` integration and do not
compile this design-time declaration.

`DesignTimeResourceConsumerTests` uses the existing independent SDK consumer
boundary. After a successful build it requests compiler arguments with
`DesignTimeBuild=true`, `BuildingProject=false`, `SkipCompilerExecution=true` and
`ProvideCommandLineArgs=true`. It checks:

- Correct Reference Resource SDK manifest metadata in the compiler-visible map.
- Content-sensitive resource inputs and a generated dependency in compiler arguments.
- Stable unchanged dependency contents and timestamp.
- Changed Reference Resource content even when its timestamp is preserved.
- Localized Resource correction, addition outside `ExpectedCultures` and excluded
  from SDK embedding, and Expected Culture removal/restoration.
- Unchanged consumer C# throughout those resource mutations.

The new regression initially failed because no content-sensitive inputs were
registered. A subsequent metadata assertion failed against the incomplete
design-time naming preparation. Both pass with the final targets. All previous
tests and assertions remain. The new inventory is 32 fast / 67 full.

Reproduce the automated check from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~DesignTimeResourceConsumerTests
pwsh -NoProfile -File tests/run.ps1 -Mode full
npm run format:check
```

These commands explicitly request builds. They prove the compiler-input contract
and existing behavior, not automatic IDE refresh.

## Live IDE observations

The prototype uses the installed VS Code extension host and actual
`vscode.executeCompletionItemProvider`, `vscode.executeHoverProvider` and
`vscode.languages.getDiagnostics` services. It does not emulate Metalama.
It filters out word suggestions when establishing the generated API baseline.

| Configuration / observation | Result |
| --- | --- |
| C# Dev Kit enabled | Project loading fails with `StreamJsonRpc.RemoteMethodNotFoundException` for `AddAdditionalFilesAsync`: deserializing `IReadOnlyList<SourceFileInfo>` fails. No valid editor baseline is established. |
| Standalone C#, target without `partial`, after resource-map fix | `LAMA0048` says the target must be `partial` to reference introduced members. Hover cannot expose the generated API. Ordinary compilation succeeds. |
| Standalone C#, initial `partial` target and valid Resource Set | Hover exposes `string Texts.FormatWelcome(string name) (+ 2 overloads)`. |
| Edit only Reference Resource to `Hello {person@string?}` | After 90 seconds, hover and completion still expose `string name`; no Localized Resource mismatch diagnostic appears. The generated dependency file is not rewritten automatically. |
| Separate manual design-time control after the failed automatic observation | A requested design-time build updates the C# dependency; the live IDE then reports `TRESX004` for the `person` Placeholder Contract mismatch. The previous generated API remains cached while validation fails. This is diagnostic evidence only. |
| Correct Localized Resource, introduce/correct key mismatch, add associated resource, remove/restore Expected Culture | Not established after the failed automatic refresh; these acceptance requirements remain open. |
| Human observation and review | Outstanding. |

The resource-only observation used no C# edit, build, restart, reload or manual
recompilation. A separate optional forced design-time control is labeled as such
and is never counted as acceptance. A `LAMA0301` wrapper may display a new custom
diagnostic's `TRESX` ID and message when the IDE cannot register its ID in the
current session; record the actual displayed ID and full message.

The retained [extension-host trace](07-vscode-csharp-control.json) records the
successful initial hover at 15:30:37 UTC, failed automatic refresh at 15:32:07 UTC,
and successful manual diagnostic control at 15:32:12 UTC. This trace requires human
review and does not complete any automatic-refresh criterion.

The separate [non-partial target trace](07-vscode-nonpartial.json) retains the
observed `LAMA0048` limitation. The final default-configuration rerun retained
the [C# Dev Kit project-loading error](07-vscode-devkit-error.txt); it failed before
the initial generated API could be observed, as shown in its
[extension-host trace](07-vscode-devkit.json).

Release solution build, all 67 tests through `tests/run.ps1 -Mode full`, and
`npm run format:check` passed. The two known ConsumerFixture identifier warnings
remain intentional. No generated build outputs are committed.
Selected diagnostic excerpts and extension-host service observations are retained;
full IDE logs remain under ignored `test-results/`.

## Reproduce the live probe

The Windows feasibility probe is kept under `prototypes/ide-refresh/`, separate
from the solution's test runner. It needs the installed `code` CLI and C# extensions,
and adds no package dependencies. It uses a fresh consumer and isolated user
profile under ignored `test-results/`, disables unrelated extensions and auto
updates in that profile, and writes `consumer/evidence.json` plus IDE logs.
The launcher returns after starting VS Code; its exit code reports launch/setup,
not the asynchronous acceptance result. Inspect the JSON `passed` values and
the `finished` record.

```powershell
# Installed C# Dev Kit configuration.
pwsh -NoProfile -File prototypes/ide-refresh/launch.ps1

# Isolate the standalone C# language server.
pwsh -NoProfile -File prototypes/ide-refresh/launch.ps1 -CSharpOnly

# Separate diagnostic control after automatic refresh fails; not acceptance.
pwsh -NoProfile -File prototypes/ide-refresh/launch.ps1 -CSharpOnly -DesignTimeControl
```

The probe stops if the initial API is unavailable or automatic Reference Resource
refresh fails. It does not report later scenarios as passing when their required
preconditions were not observed.

## Human verification sequence

Prepare a fresh valid consumer and open the directory printed by this command
in VS Code. Record the exact enabled extension versions and configuration first.

```powershell
pwsh -NoProfile -File prototypes/ide-refresh/launch.ps1 -PrepareOnly
```

After confirming the initial generated API, keep `Program.cs` unchanged for all
observations. Save each resource mutation and allow background analysis to settle.
Record elapsed time, hover/completion signatures and diagnostics with paths and
Resource Keys. Do not build, reload, restart or edit C# between steps.

1. Add Resource Key `Added` with Translation `New text` to only the Reference
   Resource. Observe the generated `Added` overloads and the Localized Resource
   missing-key diagnostic. Add the same key to the `es` resource and observe
   clearance. Rename it in the Reference Resource and verify the old API disappears
   and the new API appears, then align the Localized Resource.
2. Change `Welcome` to `Hello {person@string?}` in only the Reference Resource.
   Observe the new formatting parameter name and nullability and the Localized
   Resource Placeholder Contract diagnostic. Correct `es` to `Hola {person}` and
   observe clearance. Introduce `{other}` in `es`, observe the mismatch, then correct
   it again and observe clearance.
3. Add `Texts.fr.resx` with the same current keys but `{other}` for `Welcome`.
   `fr` is outside `ExpectedCultures`. Observe discovery and `TRESX004`, correct its
   Placeholder Contract, and observe clearance. Remove this optional resource and
   observe updated discovery without an Expected Culture error.
4. Remove `Texts.es.resx`. Observe `TRESX005` for Expected Culture `es`. Restore its
   valid current contents and observe clearance.
5. Append actual results and reviewer identity under the issue's Comments.
   Keep each unsuccessful or unobserved acceptance requirement unchecked.

The `partial` workaround does not authorize narrowing the parent specification's
requirement that a partial declaration is unnecessary. Non-partial IDE support
remains an unmet requirement in addition to automatic refresh and human review.
The installed C# Dev Kit failure is another material limitation. Further integration
work or a functioning IDE project-system configuration is required before issue 07
can be resolved.

## Code review

Both independent review axes used task-start commit
`ae673af8f3340fd193f892edad38b143c0c23e58` and the staged implementation diff.

### Standards

No documented-standard breaches or baseline code smells were found. The test uses
the integration boundary and repository naming conventions; tracker state and
Comments follow the local issue conventions; the probe is under `prototypes/`.
The reviewer noted that raw evidence for C# Dev Kit loading and `LAMA0048` was
initially absent. Both traces and the Dev Kit error excerpt are now retained above.

### Spec

Three partial or unmet areas remain: automatic API/diagnostic refresh and human
review; IDE support for the non-partial class shape; and automated coverage limited
to design-time inputs rather than refreshed editor output. The issue keeps these
limits explicit and remains unresolved. The reviewer found no scope creep and no
separate definite code-level defect. The manual control does not satisfy acceptance.
