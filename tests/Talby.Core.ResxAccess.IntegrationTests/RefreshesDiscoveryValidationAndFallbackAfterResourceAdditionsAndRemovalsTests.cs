namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition(
    "Incremental history: RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals"
)]
public sealed class RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsCollection { }

[Trait("Category", "Integration")]
[Collection(
    "Incremental history: RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals"
)]
public sealed class RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests
{
    [Fact]
    public Task RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals() =>
        IncrementalBuildConsumerHistories.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals();
}
