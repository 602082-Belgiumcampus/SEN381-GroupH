using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.RequestIntake;

public sealed record CategoryOption(Guid Id, string Name);

public sealed record SubmitRequestResult(Guid? RequestId, string? Reference, IReadOnlyList<FieldError> Errors, bool IsForbidden = false)
{
    public bool Succeeded => Reference is not null;

    public static SubmitRequestResult Created(Guid requestId, string reference) =>
        new(requestId, reference, Array.Empty<FieldError>());

    public static SubmitRequestResult Invalid(IReadOnlyList<FieldError> errors) => new(null, null, errors);

    public static SubmitRequestResult Forbidden() =>
        new(null, null, [new FieldError(string.Empty, "Not permitted.")], IsForbidden: true);
}

/// <summary>
/// Request Intake contract (ADR-008). The reference is returned only after the request is committed (NFR-007).
/// </summary>
public interface ISubmitRequest
{
    Task<IReadOnlyList<CategoryOption>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    Task<SubmitRequestResult> SubmitAsync(Guid requesterId, SubmitRequestInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// FR-020: staff log a request for someone else. The requester is found by email, or a requester
    /// account without a password is created for them (OD-07). The request records who logged it and
    /// the channel of first contact. Only staff may call this (TC-IN-04).
    /// </summary>
    Task<SubmitRequestResult> SubmitOnBehalfAsync(Actor staff, OnBehalfInput input, CancellationToken cancellationToken = default);
}

/// <summary>How the requester first got in touch (FR-020, CR-002). "Web" is used when they submit themselves.</summary>
public static class ContactChannels
{
    public const string Web = "Web";
    public const string Phone = "Phone";
    public const string Email = "Email";
    public const string WalkIn = "Walk-in";
    public const string Other = "Other";

    /// <summary>The channels staff can choose when logging on someone's behalf.</summary>
    public static readonly IReadOnlyList<string> ForStaff = [Phone, Email, WalkIn, Other];
}

/// <summary>What staff enter when logging a request on someone's behalf.</summary>
public sealed record OnBehalfInput(string? RequesterEmail, string? RequesterName, string? ContactChannel, SubmitRequestInput Request);

/// <summary>FR-020 rules that do not need the database.</summary>
public static class OnBehalfRules
{
    public const int NameMaxLength = 100;

    public static bool CanLogOnBehalf(Actor actor) =>
        actor.IsInRole(Role.ServiceStaff) || actor.IsInRole(Role.Supervisor);

    /// <summary>Checks the requester and channel fields. <paramref name="nameNeeded"/> is true when no account exists yet.</summary>
    public static IReadOnlyList<FieldError> Validate(OnBehalfInput input, bool nameNeeded)
    {
        var errors = new List<FieldError>();

        var email = input.RequesterEmail?.Trim();
        if (string.IsNullOrEmpty(email))
            errors.Add(new(nameof(OnBehalfInput.RequesterEmail), "Enter the requester's email address."));
        else if (!LooksLikeEmail(email))
            errors.Add(new(nameof(OnBehalfInput.RequesterEmail), "Enter a valid email address."));

        if (nameNeeded)
        {
            if (string.IsNullOrWhiteSpace(input.RequesterName))
                errors.Add(new(nameof(OnBehalfInput.RequesterName), "No account uses that email yet. Enter the requester's name to create one."));
            else if (input.RequesterName.Trim().Length > NameMaxLength)
                errors.Add(new(nameof(OnBehalfInput.RequesterName), $"Keep the name to {NameMaxLength} characters or fewer."));
        }

        if (input.ContactChannel is null || !ContactChannels.ForStaff.Contains(input.ContactChannel))
            errors.Add(new(nameof(OnBehalfInput.ContactChannel), "Choose how the requester contacted you."));

        return errors;
    }

    // Deliberately simple: one @ with text either side and a dot in the domain
    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@') && email.IndexOf('.', at) > at + 1 && !email.EndsWith('.') && !email.Contains(' ');
    }
}
