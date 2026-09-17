namespace AethericContracts.Interactions;

/// <summary>A trusted host resolves providers in its authenticated request/circuit scope.</summary>
public interface IInteractionProvider
{
    string Id { get; }
    IInteractionSession Open(string subjectId, string actionId);
}

/// <summary>
/// A server-side, single-subject interaction session. Implementations authorize every operation.
/// Preparing returns an opaque confirmation token for an immutable input snapshot. Confirming the same
/// token must retry the same operation idempotently, including after an uncertain transport failure.
/// Tokens do not grant authority; confirm rechecks the current caller. Never deserialize a session.
/// </summary>
public interface IInteractionSession
{
    Task<InteractionView> LoadAsync(CancellationToken cancellationToken = default);
    Task<InteractionView> PrepareAsync(IReadOnlyDictionary<string, string> inputs,
        CancellationToken cancellationToken = default);
    Task<InteractionResult> ConfirmAsync(string confirmationToken,
        CancellationToken cancellationToken = default);
}

/// <summary>Plain text only. Renderers must encode every string; no HTML or executable UI is supplied.</summary>
public sealed record InteractionSection(string Heading, string Text);

/// <summary>Presentation hints; the operation remains authoritative for validation.</summary>
public sealed record InteractionTextField(string Id, string Label, string Help,
    bool Required, int MaxLength, string Value = "");

public sealed record InteractionMessage(string Text, string? FieldId = null);

public sealed record InteractionConfirmation(string Token, string Prompt, string SubmitLabel,
    IReadOnlyList<InteractionSection> Sections);

/// <summary>
/// The first vocabulary supports a text form followed by explicit confirmation. IsAvailable=false
/// means no protected record or fields may be shown. CanEdit=false preserves an uncertain operation
/// until its confirmation is retried. View data and its token belong to this session only.
/// </summary>
public sealed record InteractionView(string Title, string Description,
    IReadOnlyList<InteractionSection> Sections, IReadOnlyList<InteractionTextField> Fields,
    string PrepareLabel, IReadOnlyList<InteractionMessage> Messages,
    bool IsAvailable = true, InteractionConfirmation? Confirmation = null, bool CanEdit = true);

public enum InteractionResultKind
{
    Completed,
    Invalid,
    Conflict,
    Unavailable,
    RetryableFailure
}

/// <summary>Unavailable clears protected display data. RetryableFailure retains the exact confirmation.</summary>
public sealed record InteractionResult(InteractionResultKind Kind, IReadOnlyList<InteractionMessage> Messages);
