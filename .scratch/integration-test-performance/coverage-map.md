# IntegrationTests variant coverage map

The generator freezes baseline, A, B and AB from one git HEAD archive. It
copies all current test source and assertions into every tree before applying
the option patches. The six incremental histories are the only changed test
identities in A and AB. Their helper code is mechanically changed from an
instance test class to a static history class; the generator reverses those
declaration changes and requires the normalized source SHA-256 to match.

## Changed identities

| Original baseline identity | A and AB identity | Retained scenario and assertions |
| --- | --- | --- |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits | Talby.Core.ResxAccess.IntegrationTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits | Asserts exact raw and formatted output for invariant and Spanish resources; verifies a no-op build preserves the assembly timestamp; edits the reference and localized resources in order and verifies each runtime output. |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals | Talby.Core.ResxAccess.IntegrationTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals | Verifies expected-culture fallback, a newly discovered Spanish resource, exact TRESX004 resource path/key/placeholder-contract text for an invalid French resource, recovery after correction, and fallback after Spanish-resource removal. |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration | Talby.Core.ResxAccess.IntegrationTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration | Verifies initial output with expected Spanish, exact TRESX005 missing-culture text after removal, and successful output after restoration. |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.DetectsAssociatedResourcesExcludedFromSdkEmbedding | Talby.Core.ResxAccess.IntegrationTests.DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.DetectsAssociatedResourcesExcludedFromSdkEmbedding | Verifies fallback without French, exact TRESX004 placeholder-contract details for an invalid French resource, exact SDK satellite-embedding diagnostic after fixing its content, and recovery after removal. |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics | Talby.Core.ResxAccess.IntegrationTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics | Verifies generated keys and reflected signatures, raw and formatted output, nullable and typed arguments, exact TRESX004 messages for changed contracts, malformed type declarations and key-set mismatch, plus successful output after each repair and key rename. |
| Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources | Talby.Core.ResxAccess.IntegrationTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources | Verifies omitted-key handling, unchanged output after an unrelated resource addition, exact TRESX004 omitted-key contract text, exact TRESX001 unsupported-type text, and recovery after both repairs. |

The generated identity-map.json in A and AB contains these six new-to-baseline
mappings. Baseline and B use an empty dictionary; unchanged identities are
normalized as identity mappings by the comparison harness.

## Unchanged test identities

These 29 test methods retain their original names and bodies in all trees.
Their assertions remain byte-for-byte as copied from the same HEAD archive.

| Existing class | Retained methods |
| --- | --- |
| RawTextConsumerTests | CanCompileAndInvokeIndependentResourceSets; ReportsEachInvalidResourceAndEmbeddingInOneBuild; ReportsMalformedReferenceResourceWithoutAspectCrash; DescribesMissingRuntimeManifestAndResourceKey |
| LocalizedResourceConsumerTests | CanInvokeSatelliteResourcesWithDefaultAndExplicitCulture; RejectsUnsupportedLocalizedResourceCultureCasing; CanInvokeCanonicalAndLowercaseLocalizedResourceCultures; RejectsInconsistentLocalizedResourcesOutsideExpectedCultures; ReportsMissingAndInvalidExpectedCultures; RejectsLocalizedResourcesWithoutStandardSatelliteEmbedding |
| NamedPlaceholderConsumerTests | GeneratesNamedArgumentsBeforeOnlyUsedIndexedIdentities; PreservesEverySupportedArgumentTypeAndNullableRequiredSignature; FormatsUntypedNullableKeywordAndUnicodeArgumentsWithStandardSemantics; SelectsIndependentCulturesAndReorderedFormatsForEveryNamedOverload; RejectsInvalidNamedDeclarationsAndParameterCollisionsPrecisely; RejectsChangedNamedAndMixedContractsIncludingNullabilityAndOmittedKeys; ConsumerCompilerEnforcesNamedTypesNullabilityAndRequiredArguments |
| IndexedPlaceholderConsumerTests | GeneratesRequiredNullableArgumentsInNumericOrderWithIndexGaps; SelectsResourceAndFormattingCulturesIndependentlyForAllOverloads; FormatsAlignmentEscapedBracesAndNullArgumentsWhilePreservingRawText; PropagatesStandardFormattingFailuresAndRejectsNullCultures; RejectsMalformedIndexedPlaceholdersWithResourceAndKeyDiagnostics; RejectsChangedPlaceholderContractsInEveryLocalizedResourceAndOmittedKey |
| ResourceKeyIdentifierConsumerTests | NormalizesReproduciblyAndInvokesOriginalKeysAndEscapedKeywords; RejectsExistingMembersAndGeneratedMemberFamilyCollisions; WarnAndIgnoreOmitTheSameMembersWithDistinctDiagnostics; OmittedKeysStillReportResourceSetAndPlaceholderContractErrorsInBothPolicies; RejectsUnsupportedIdentifierPolicies |
| DesignTimeResourceConsumerTests | ExposesResourceChangesAsWatchedDesignTimeCompilationInputs |

The five classes that consume ConsumerDiagnosticsFixture remain in the single
SDK consumer builds collection: RawText, LocalizedResource, NamedPlaceholder,
IndexedPlaceholder and ResourceKeyIdentifier. This retains one shared fixture
instance and its two diagnostic builds. The DesignTime test keeps its identity
and moves to its own collection. All other culture-sensitive tests remain in
the existing shared collection.

## Execution boundaries

- In A and AB, each history gets a separate xUnit collection, so xUnit can
  schedule up to two independent histories concurrently. All operations
  inside a history still await in their original order.
- The existing diagnostic-fixture collection remains intact. Its fixture can
  run two child builds while one independent history also runs, so the
  observed child-process concurrency can reach three. Collection worker count
  alone does not prove the process limit; use the opt-in traces during an
  excluded probe.
- B and AB keep the original first dotnet build, which performs the initial
  implicit restore. A consumer-local AfterTargets=Restore target writes a
  success marker beneath that consumer's obj directory. A later build adds
  --no-restore only when the marker exists and the Consumer.csproj
  fingerprint is unchanged. Writes through ConsumerProject.Write to project,
  props, targets, solution, NuGet config, package config or lock files clear
  the marker. Source and resource writes do not.
- A failed compilation does not clear a successful restore marker. A failed
  restore does not run the marker target, so a later build attempts restore
  again. The implementation never treats project.assets.json or a zero build
  exit code as proof of successful restore.
