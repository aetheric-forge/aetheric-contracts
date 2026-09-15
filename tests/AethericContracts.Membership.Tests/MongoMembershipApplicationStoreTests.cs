using MongoDB.Driver;

namespace AethericContracts.Membership.Tests;

/// <summary>
/// Points at a locally reachable mongod with auth disabled, using a fresh throwaway database per
/// test run (dropped on teardown) - not Testcontainers-based, some dev/CI environments here can't
/// run nested Docker containers, so tests target a directly reachable mongod instead.
/// </summary>
public sealed class MongoMembershipApplicationStoreTests : IAsyncLifetime
{
    private readonly string databaseName = $"aetheric-contracts-tests-{Guid.NewGuid():N}";
    private MongoClient client = default!;
    private MongoMembershipApplicationStore store = default!;

    public Task InitializeAsync()
    {
        var host = Environment.GetEnvironmentVariable("MONGO_TEST_HOST") ?? "localhost";
        client = new MongoClient($"mongodb://{host}:27017/?directConnection=true");
        var database = client.GetDatabase(databaseName);
        // The [FromKeyedServices] attribute on the constructor is DI metadata only - it doesn't
        // stop us from constructing the store directly with a plain IMongoDatabase in tests.
        store = new MongoMembershipApplicationStore(database);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await client.DropDatabaseAsync(databaseName);

    private static MembershipApplication CreateApplication() => new()
    {
        DisplayName = "Octavia Blake",
        Email = "octavia@example.com",
        RequestedUsername = "octavia",
        Statement = new string('a', 30),
        Consent = true
    };

    [Fact]
    public async Task SubmitAsync_ThenFindAsync_RoundTrips()
    {
        var application = CreateApplication();

        await store.SubmitAsync(application);
        var found = await store.FindAsync(application.Id);

        Assert.NotNull(found);
        Assert.Equal(application.Email, found!.Email);
    }

    [Fact]
    public async Task SaveAsync_UpsertsAndUpdates()
    {
        var application = CreateApplication();
        await store.SubmitAsync(application);

        application.Status = MembershipApplicationStatus.Approved;
        application.ReviewedBy = "dean";
        await store.SaveAsync(application);

        var found = await store.FindAsync(application.Id);
        Assert.Equal(MembershipApplicationStatus.Approved, found!.Status);
        Assert.Equal("dean", found.ReviewedBy);
    }

    [Fact]
    public async Task ListAsync_ReturnsOrderedBySubmittedAtDescending()
    {
        var older = CreateApplication();
        await store.SubmitAsync(older);
        await Task.Delay(10);
        var newer = CreateApplication();
        await store.SubmitAsync(newer);

        var all = await store.ListAsync();

        Assert.Equal(newer.Id, all[0].Id);
        Assert.Equal(older.Id, all[1].Id);
    }
}
