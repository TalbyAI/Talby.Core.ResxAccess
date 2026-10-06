namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition(
    "Incremental history: RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration"
)]
public sealed class RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration")]
public sealed class RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests
{
    [Fact]
    public Task RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration() =>
        IncrementalBuildConsumerHistories.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration();
}
