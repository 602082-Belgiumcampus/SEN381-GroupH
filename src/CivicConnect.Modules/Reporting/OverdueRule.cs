using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Modules.Reporting;

/// <summary>
/// CR-001 / FR-015: a request is overdue when it is open (not Resolved, Closed or Rejected) and the
/// time since it was created exceeds its category's target resolution period, in calendar days
/// (AS-06). Exactly at the target is not yet overdue. Overdue is calculated, never stored.
/// </summary>
public static class OverdueRule
{
    public static bool IsOpen(RequestStatus status) =>
        status is not (RequestStatus.Resolved or RequestStatus.Closed or RequestStatus.Rejected);

    public static bool IsOverdue(RequestStatus status, DateTimeOffset createdAt, int targetResolutionDays, DateTimeOffset now) =>
        IsOpen(status) && now > DueAt(createdAt, targetResolutionDays);

    public static DateTimeOffset DueAt(DateTimeOffset createdAt, int targetResolutionDays) =>
        createdAt.AddDays(targetResolutionDays);
}
