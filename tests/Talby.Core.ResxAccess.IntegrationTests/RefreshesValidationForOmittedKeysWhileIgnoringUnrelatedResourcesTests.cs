namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition(
    "Incremental history: RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources"
)]
public sealed class RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesCollection { }

[Trait("Category", "Integration")]
[Collection(
    "Incremental history: RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources"
)]
public sealed class RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests
{
    [Fact]
    public Task RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources() =>
        IncrementalBuildConsumerHistories.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources();
}
