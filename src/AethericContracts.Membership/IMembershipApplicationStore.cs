namespace AethericContracts.Membership;

public interface IMembershipApplicationStore
{
    Task<MembershipApplication> SubmitAsync(MembershipApplication application, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembershipApplication>> ListAsync(CancellationToken cancellationToken = default);
    Task<MembershipApplication?> FindAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAsync(MembershipApplication application, CancellationToken cancellationToken = default);
}
