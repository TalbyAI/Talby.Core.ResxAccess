namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: DetectsAssociatedResourcesExcludedFromSdkEmbedding")]
public sealed class DetectsAssociatedResourcesExcludedFromSdkEmbeddingCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: DetectsAssociatedResourcesExcludedFromSdkEmbedding")]
public sealed class DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests
{
    [Fact]
    public Task DetectsAssociatedResourcesExcludedFromSdkEmbedding() =>
        IncrementalBuildConsumerHistories.DetectsAssociatedResourcesExcludedFromSdkEmbedding();
}
