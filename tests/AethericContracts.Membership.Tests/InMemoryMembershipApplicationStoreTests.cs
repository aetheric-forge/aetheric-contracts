namespace AethericContracts.Membership.Tests;

public sealed class InMemoryMembershipApplicationStoreTests
{
    private static MembershipApplication CreateApplication(
        string username = "octavia",
        DateTimeOffset? submittedAt = null) => new()
    {
        DisplayName = "Octavia Blake",
        Email = "octavia@example.com",
        RequestedUsername = username,
        Statement = new string('a', 30),
        Consent = true,
        SubmittedAt = submittedAt ?? DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task SubmitAsync_ThenFindAsync_RoundTrips()
    {
        var store = new InMemoryMembershipApplicationStore();
        var application = CreateApplication();

        await store.SubmitAsync(application);
        var found = await store.FindAsync(application.Id);

        Assert.NotNull(found);
        Assert.Equal(application.RequestedUsername, found!.RequestedUsername);
    }

    [Fact]
    public async Task SubmitAsync_RejectsDuplicateId()
    {
        var store = new InMemoryMembershipApplicationStore();
        var application = CreateApplication();
        await store.SubmitAsync(application);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.SubmitAsync(application));
    }

    [Fact]
    public async Task ListAsync_OrdersBySubmittedAtDescending()
    {
        var store = new InMemoryMembershipApplicationStore();
        var older = CreateApplication("older", DateTimeOffset.UtcNow.AddMinutes(-5));
        await store.SubmitAsync(older);
        var newer = CreateApplication("newer");
        await store.SubmitAsync(newer);

        var all = await store.ListAsync();

        Assert.Equal(newer.Id, all[0].Id);
        Assert.Equal(older.Id, all[1].Id);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingApplication()
    {
        var store = new InMemoryMembershipApplicationStore();
        var application = CreateApplication();
        await store.SubmitAsync(application);

        application.Status = MembershipApplicationStatus.Approved;
        await store.SaveAsync(application);

        var found = await store.FindAsync(application.Id);
        Assert.Equal(MembershipApplicationStatus.Approved, found!.Status);
    }
}
