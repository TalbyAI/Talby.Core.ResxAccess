namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition(
    "Incremental history: RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics"
)]
public sealed class RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics")]
public sealed class RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests
{
    [Fact]
    public Task RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics() =>
        IncrementalBuildConsumerHistories.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics();
}
