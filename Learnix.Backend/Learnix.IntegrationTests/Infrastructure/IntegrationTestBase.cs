namespace Learnix.IntegrationTests.Infrastructure;

/// <summary>
/// Binds every integration test to one shared <see cref="LearnixApp"/>, so the Docker containers start
/// once for the whole run rather than per class. xUnit runs classes in one collection sequentially,
/// which is what lets each test own a clean database (see <see cref="IntegrationTestBase"/>).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<LearnixApp>
{
    public const string Name = "integration";
}

/// <summary>
/// Base for integration tests: hands out the shared app and truncates the database before each test, so
/// tests neither depend on order nor leak rows into one another.
/// </summary>
[Collection(IntegrationCollection.Name)]
public abstract class IntegrationTestBase(LearnixApp app) : IAsyncLifetime
{
    protected LearnixApp App { get; } = app;

    public Task InitializeAsync() => App.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
