using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.RequestIntake;

/// <summary>
/// Request Intake (FR-001 to FR-003, NFR-007). Validates on the server, then saves the request and its
/// "Submitted" audit entry in one transaction. The reference is returned only after the commit.
/// </summary>
public sealed class SubmitRequestService(CivicConnectDbContext db, TimeProvider time) : ISubmitRequest
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
        var activeCategoryIds = (await db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var errors = SubmitRequestRules.Validate(input, activeCategoryIds);
        if (errors.Count > 0)
            return SubmitRequestResult.Invalid(errors);

        var clean = SubmitRequestRules.Normalise(input);
        var now = time.GetUtcNow();
        var reference = RequestReference.Format(now.Year, await NextReferenceNumberAsync(cancellationToken));

        var request = new ServiceRequest
        {
            Id = Guid.CreateVersion7(now),
            Reference = reference,
            RequesterId = requesterId,
            CategoryId = clean.CategoryId!.Value,
            ContactChannel = ServiceRequest.WebChannel,
            Title = clean.Title!,
            Description = clean.Description!,
            Location = clean.Location,
            Status = RequestStatus.Submitted,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.ServiceRequests.Add(request);
        db.AuditEntries.Add(new AuditEntry
        {
            RequestId = request.Id,
            ActorId = requesterId,
            EventType = "Submit",
            FromStatus = null,
            ToStatus = RequestStatus.Submitted,
            Detail = $"reference={reference}",
            OccurredAt = now
        });

        // One SaveChanges call runs in one transaction: both rows are saved or neither is.
        await db.SaveChangesAsync(cancellationToken);

        return SubmitRequestResult.Created(request.Id, reference);
    }

    // Fixed SQL with no user input. EF Core expects a single scalar column to be named "Value".
    private const string NextReferenceSql =
        "SELECT nextval('\"" + CivicConnectDbContext.ReferenceSequence + "\"') AS \"Value\"";

    private Task<long> NextReferenceNumberAsync(CancellationToken cancellationToken) =>
        db.Database.SqlQueryRaw<long>(NextReferenceSql).SingleAsync(cancellationToken);
}
