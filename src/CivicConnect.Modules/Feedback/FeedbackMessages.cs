using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Modules.Feedback;

/// <summary>
/// FR-006: the text of the notice a requester receives for each agreed feedback event (CL-02):
/// accepted, rejected (with the reason), status updated, and resolved or closed.
/// Returns null for events that do not notify the requester.
/// </summary>
public static class FeedbackMessages
{
    public const int MessageMaxLength = 500;

    public static string? ForTransition(LifecycleEvent lifecycleEvent, string reference, string? detail) =>
        Shorten(lifecycleEvent switch
        {
            LifecycleEvent.Accept => $"Your request {reference} was accepted.",
            LifecycleEvent.Reject => $"Your request {reference} was not accepted. Reason: {detail}",
            LifecycleEvent.Assign => $"Your request {reference} has been given to a staff member.",
            LifecycleEvent.Start => $"Work has started on your request {reference}.",
            LifecycleEvent.Resolve => $"Your request {reference} has been resolved. What was done: {detail}",
            LifecycleEvent.Close => $"Your request {reference} is now closed.",
            LifecycleEvent.Reopen => $"Your request {reference} was reopened and work continues.",
            _ => null
        });

    public static string LoggedOnBehalf(string reference, string channel) =>
        Shorten($"A staff member logged request {reference} for you (contact by {channel.ToLowerInvariant()}).")!;

    private static string? Shorten(string? message) =>
        message is { Length: > MessageMaxLength } ? message[..(MessageMaxLength - 1)] + "…" : message;
}
