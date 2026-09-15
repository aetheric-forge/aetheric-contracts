using System.ComponentModel.DataAnnotations;

namespace AethericContracts.Membership;

public enum MembershipApplicationStatus
{
    Submitted,
    UnderReview,
    Approved,
    Declined
}

public enum IdentityProvisioningStatus
{
    NotRequested,
    Pending,
    InProgress,
    Provisioned,
    Failed
}

public sealed class MembershipApplication
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 3)]
    public string RequestedUsername { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 20)]
    public string Statement { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must consent to the processing of this application.")]
    public bool Consent { get; set; }

    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public MembershipApplicationStatus Status { get; set; } = MembershipApplicationStatus.Submitted;
    public IdentityProvisioningStatus ProvisioningStatus { get; set; } = IdentityProvisioningStatus.NotRequested;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public string? ExternalIdentityId { get; set; }
    public string? ProvisioningError { get; set; }
}
