namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition(
    "Incremental history: RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits"
)]
public sealed class RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits")]
public sealed class RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests
{
    [Fact]
    public Task RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits() =>
        IncrementalBuildConsumerHistories.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits();
}
