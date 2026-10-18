using CivicConnect.Infrastructure.Identity;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.RequestIntake;

/// <summary>
/// Request Intake (FR-001 to FR-003, FR-020, NFR-007). Validates on the server, then saves the request
/// and its audit entry in one transaction. The reference is returned only after the commit.
/// </summary>
public sealed class SubmitRequestService(CivicConnectDbContext db, UserManager<AppUser> users, TimeProvider time) : ISubmitRequest
{
    public async Task<IReadOnlyList<CategoryOption>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default) =>
        await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption(c.Id, c.Name))
            .ToListAsync(cancellationToken);

    public async Task<SubmitRequestResult> SubmitAsync(
        Guid requesterId, SubmitRequestInput input, CancellationToken cancellationToken = default)
    {
        var errors = SubmitRequestRules.Validate(input, await ActiveCategoryIdsAsync(cancellationToken));
        if (errors.Count > 0)
            return SubmitRequestResult.Invalid(errors);

        var request = await AddRequestAsync(requesterId, input, loggedById: null, ContactChannels.Web, cancellationToken);
        AddAudit(request, requesterId, "Submit", $"reference={request.Reference}");

        // One SaveChanges call runs in one transaction: both rows are saved or neither is.
        await db.SaveChangesAsync(cancellationToken);
        return SubmitRequestResult.Created(request.Id, request.Reference);
    }

    public async Task<SubmitRequestResult> SubmitOnBehalfAsync(
        Actor staff, OnBehalfInput input, CancellationToken cancellationToken = default)
    {
        // Authorise before looking anything up (TC-IN-04)
        if (!OnBehalfRules.CanLogOnBehalf(staff))
            return SubmitRequestResult.Forbidden();

        var email = input.RequesterEmail?.Trim() ?? string.Empty;
        var existing = string.IsNullOrEmpty(email) ? null : await users.FindByEmailAsync(email);

        var errors = new List<FieldError>(OnBehalfRules.Validate(input, nameNeeded: existing is null));
        errors.AddRange(SubmitRequestRules.Validate(input.Request, await ActiveCategoryIdsAsync(cancellationToken)));

        if (existing is not null && !await IsActiveRequesterAsync(existing))
            errors.Add(new(nameof(OnBehalfInput.RequesterEmail),
                "That email belongs to an account that is not an active requester. Use the requester's own email."));

        if (errors.Count > 0)
            return SubmitRequestResult.Invalid(errors);

        // The new account (if any), the request, its audit entry and the notice commit together
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var requester = existing ?? await CreatePasswordlessRequesterAsync(email, input.RequesterName!.Trim());
        var channel = input.ContactChannel!;

        var request = await AddRequestAsync(requester.Id, input.Request, staff.UserId, channel, cancellationToken);
        AddAudit(request, staff.UserId, "LoggedOnBehalf", $"reference={request.Reference}; channel={channel}");
        db.FeedbackNotices.Add(new FeedbackNotice
        {
            RequestId = request.Id,
            RecipientId = requester.Id,
            Message = FeedbackMessages.LoggedOnBehalf(request.Reference, channel),
            CreatedAt = request.CreatedAt
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return SubmitRequestResult.Created(request.Id, request.Reference);
    }

    private async Task<IReadOnlySet<Guid>> ActiveCategoryIdsAsync(CancellationToken cancellationToken) =>
        (await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    private async Task<bool> IsActiveRequesterAsync(AppUser user) =>
        user.IsActive && await users.IsInRoleAsync(user, nameof(Role.Requester));

    /// <summary>
    /// OD-07: a person without an account gets a requester account with no password. They cannot sign
    /// in until they register with the same email, which then sets their password (see Register).
    /// </summary>
    private async Task<AppUser> CreatePasswordlessRequesterAsync(string email, string name)
    {
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            DisplayName = name,
            IsActive = true,
            CreatedAt = time.GetUtcNow()
        };

        ThrowIfFailed(await users.CreateAsync(user), "create the requester account");
        ThrowIfFailed(await users.AddToRoleAsync(user, nameof(Role.Requester)), "give the account the Requester role");
        return user;
    }

    private async Task<ServiceRequest> AddRequestAsync(
        Guid requesterId, SubmitRequestInput input, Guid? loggedById, string channel, CancellationToken cancellationToken)
    {
        var clean = SubmitRequestRules.Normalise(input);
        var now = time.GetUtcNow();
        var reference = RequestReference.Format(now.Year, await NextReferenceNumberAsync(cancellationToken));

        var request = new ServiceRequest
        {
            Id = Guid.CreateVersion7(now),
            Reference = reference,
            RequesterId = requesterId,
            CategoryId = clean.CategoryId!.Value,
            LoggedById = loggedById,
            ContactChannel = channel,
            Title = clean.Title!,
            Description = clean.Description!,
            Location = clean.Location,
            Status = RequestStatus.Submitted,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.ServiceRequests.Add(request);
        return request;
    }

    private void AddAudit(ServiceRequest request, Guid actorId, string eventType, string detail) =>
        db.AuditEntries.Add(new AuditEntry
        {
            RequestId = request.Id,
            ActorId = actorId,
            EventType = eventType,
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            Detail = detail,
            OccurredAt = request.CreatedAt
        });

    private static void ThrowIfFailed(IdentityResult result, string what)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not {what}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    // Fixed SQL with no user input. EF Core expects a single scalar column to be named "Value".
    private const string NextReferenceSql =
        "SELECT nextval('\"" + CivicConnectDbContext.ReferenceSequence + "\"') AS \"Value\"";

    private Task<long> NextReferenceNumberAsync(CancellationToken cancellationToken) =>
        db.Database.SqlQueryRaw<long>(NextReferenceSql).SingleAsync(cancellationToken);
}
