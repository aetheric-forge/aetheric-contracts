using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
    private static int guidRepresentationRegistered;

    private readonly IMongoCollection<MembershipApplication> _collection;

    public MongoMembershipApplicationStore([FromKeyedServices("Membership")] IMongoDatabase database)
    {
        EnsureGuidRepresentationRegistered();
        _collection = database.GetCollection<MembershipApplication>("membership-applications");
    }

    /// <summary>
    /// MongoDB.Driver 3.x removed its old implicit Guid-serialization default - without this, every
    /// write throws "GuidSerializer cannot serialize a Guid when GuidRepresentation is Unspecified."
    /// Registered once per process rather than per-property attributes, since MembershipApplication
    /// itself should stay free of any MongoDB-specific concerns.
    ///
    /// RegisterSerializer itself throws if a serializer is already registered for the type - not
    /// idempotent - so a host app that already registers its own Guid serializer (as aetheric-admin
    /// does, for its own Guid-keyed Mongo stores) would otherwise crash this store's first
    /// construction. This type can't know what the host already did, so it must tolerate that case
    /// rather than assume it owns the registration.
    /// </summary>
    private static void EnsureGuidRepresentationRegistered()
    {
        if (Interlocked.Exchange(ref guidRepresentationRegistered, 1) == 1)
        {
            return;
        }

        try
        {
            BsonSerializer.RegisterSerializer(new MongoDB.Bson.Serialization.Serializers.GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered (by this process's host app, or a prior instance of this store) -
            // fine either way, as long as it's Standard representation, which every writer/reader of
            // this shared collection is expected to agree on.
        }
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
