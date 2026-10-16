using System.Security.Claims;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Web.Security;

/// <summary>
/// Role policies for broad capability (ADR-007). Per-request access is decided by
/// <see cref="RequestVisibility"/> and <see cref="LifecycleAuthorisation"/> on the server.
/// </summary>
public static class Policies
{
    public const string Requester = "Requester";
    public const string StaffQueue = "StaffQueue";

    public static readonly string[] StaffQueueRoles =
        [nameof(Role.ServiceStaff), nameof(Role.Supervisor), nameof(Role.Management)];
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("The signed-in user has no id claim."));

    /// <summary>The signed-in user as the <see cref="Actor"/> the rules work with.</summary>
    public static Actor ToActor(this ClaimsPrincipal user) =>
        ActorFactory.FromRoleNames(user.GetUserId(), user.FindAll(ClaimTypes.Role).Select(c => c.Value));
}

public static class StatusDisplay
{
    public static string Label(RequestStatus status) =>
        status == RequestStatus.InProgress ? "In progress" : status.ToString();

    public static string BadgeClass(RequestStatus status) => status switch
    {
        RequestStatus.Submitted => "text-bg-secondary",
        RequestStatus.Accepted or RequestStatus.Assigned or RequestStatus.InProgress => "text-bg-primary",
        RequestStatus.Resolved or RequestStatus.Closed => "text-bg-success",
        RequestStatus.Rejected => "text-bg-danger",
        _ => "text-bg-light"
    };
}
