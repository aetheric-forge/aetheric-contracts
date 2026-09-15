using System.Collections.Concurrent;

namespace AethericContracts.Membership;

/// <summary>
/// Loses data on restart - useful for local dev/tests, not for anything that needs to survive
/// a process restart or be read by a second process. See MongoMembershipApplicationStore for that.
/// </summary>
public sealed class InMemoryMembershipApplicationStore : IMembershipApplicationStore
{
    private readonly ConcurrentDictionary<Guid, MembershipApplication> _applications = new();

    public Task<MembershipApplication> SubmitAsync(MembershipApplication application, CancellationToken cancellationToken = default)
    {
        if (!_applications.TryAdd(application.Id, application))
        {
            throw new InvalidOperationException($"Application {application.Id} already exists.");
        }

        return Task.FromResult(application);
    }

    public Task<IReadOnlyList<MembershipApplication>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MembershipApplication>>(
            _applications.Values.OrderByDescending(application => application.SubmittedAt).ToArray());

    public Task<MembershipApplication?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _applications.TryGetValue(id, out var application);
        return Task.FromResult(application);
    }

    public Task SaveAsync(MembershipApplication application, CancellationToken cancellationToken = default)
    {
        _applications[application.Id] = application;
        return Task.CompletedTask;
    }
}
