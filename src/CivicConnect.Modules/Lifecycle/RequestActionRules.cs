using CivicConnect.Modules.IdentityAccess;

namespace CivicConnect.Modules.Lifecycle;

/// <summary>
/// FR-012: recording an action taken on a request (a work-log entry). Actions do not change the
/// status, so they are not in the transition table, but they follow the same "who may" idea:
/// the assignee or a supervisor, while the request is being worked on.
/// </summary>
public static class RequestActionRules
{
    public const int TextMaxLength = 2000;

    public static bool CanRecord(Actor actor, RequestState state) =>
        state.Status is RequestStatus.Assigned or RequestStatus.InProgress
        && (state.AssignedToId == actor.UserId || actor.IsInRole(Role.Supervisor));

    public static string? Validate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Describe the action you took.";
        if (text.Trim().Length > TextMaxLength)
            return $"Keep the action to {TextMaxLength} characters or fewer.";
        return null;
    }
}
