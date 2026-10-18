using CivicConnect.Infrastructure.Identity;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Infrastructure.Persistence;

/// <summary>Controlled category list (FR-002, FR-022, CR-001).</summary>
public class Category
{
    public const int NameMaxLength = 100;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TargetResolutionDays { get; set; }
}

/// <summary>The authoritative request record (PED Section 6.2, ServiceRequest).</summary>
public class ServiceRequest
{
    public const int ReferenceMaxLength = 20;
    public const int ContactChannelMaxLength = 20;
    public const string WebChannel = "Web";

    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }
    public AppUser Requester { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? AssignedToId { get; set; }
    public AppUser? AssignedTo { get; set; }

    // CR-002 (staff log on behalf). Null when the requester submitted it themselves.
    public Guid? LoggedById { get; set; }
    public AppUser? LoggedBy { get; set; }
    public string ContactChannel { get; set; } = WebChannel;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Submitted;

    /// <summary>Optimistic concurrency token (Section 6.5). Only the Request Lifecycle changes it.</summary>
    public int Version { get; set; } = 1;

    public string? RejectionReason { get; set; }
    public string? ResolutionNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

/// <summary>FR-012: an action recorded on a request (work-log entry). Append-only, like the audit trail.</summary>
public class RequestAction
{
    public long Id { get; set; }

    public Guid RequestId { get; set; }
    public ServiceRequest Request { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>FR-006: a notice to the requester about a change to their request (CL-02).</summary>
public class FeedbackNotice
{
    public long Id { get; set; }

    public Guid RequestId { get; set; }
    public ServiceRequest Request { get; set; } = null!;

    public Guid RecipientId { get; set; }
    public AppUser Recipient { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

/// <summary>
/// Append-only record of significant actions (FR-019, NFR-008). The application never updates or
/// deletes these rows; restricting the database role to INSERT/SELECT is still to do (R-12).
/// </summary>
public class AuditEntry
{
    public const int EventTypeMaxLength = 40;

    public long Id { get; set; }

    public Guid RequestId { get; set; }
    public ServiceRequest Request { get; set; } = null!;

    public Guid ActorId { get; set; }
    public AppUser Actor { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;
    public RequestStatus? FromStatus { get; set; }
    public RequestStatus ToStatus { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
