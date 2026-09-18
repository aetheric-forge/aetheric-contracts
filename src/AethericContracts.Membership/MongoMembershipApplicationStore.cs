using Forge.Primitives.MongoDb;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace AethericContracts.Membership;

/// <summary>
/// Backs Join Campus applications with real, shared storage - the whole reason this type lives
/// here rather than in either app individually is that aetheric-web (which creates applications)
/// and aetheric-admin (which reads/flags them, e.g. the stale-application worker) are now separate
/// processes that can't share InMemoryMembershipApplicationStore's in-process state.
/// </summary>
public sealed class MongoMembershipApplicationStore : IMembershipApplicationStore
{
    private readonly IMongoCollection<MembershipApplication> _collection;

    public MongoMembershipApplicationStore([FromKeyedServices("Membership")] IMongoDatabase database)
    {
        MongoBsonSetup.EnsureGuidRepresentationRegistered();
        _collection = database.GetCollection<MembershipApplication>("membership-applications");
    }

    public async Task<MembershipApplication> SubmitAsync(
        MembershipApplication application,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(application, cancellationToken: cancellationToken).ConfigureAwait(false);
        return application;
    }

    public async Task<IReadOnlyList<MembershipApplication>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(FilterDefinition<MembershipApplication>.Empty)
            .SortByDescending(application => application.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MembershipApplication?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(application => application.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(MembershipApplication application, CancellationToken cancellationToken = default)
    {
        await _collection.ReplaceOneAsync(
                existing => existing.Id == application.Id,
                application,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken)
            .ConfigureAwait(false);
    }
}
