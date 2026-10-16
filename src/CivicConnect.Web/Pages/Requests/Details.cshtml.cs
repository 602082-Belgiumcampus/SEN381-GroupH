using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// Request detail for requesters and staff (FR-009), with the review actions Accept (T1) and
/// Reject (T2). The page only offers actions the rules allow, but every post goes through
/// <see cref="ILifecycleService"/>, which checks again (NFR-002).
/// </summary>
public class DetailsModel(IRequestQueries queries, ILifecycleService lifecycle) : PageModel
{
    public RequestDetail Detail { get; private set; } = null!;

    public bool CanAccept { get; private set; }
    public bool CanReject { get; private set; }

    /// <summary>Staff see the requester's name; requesters already know it is theirs.</summary>
    public bool ShowRequester { get; private set; }

    [BindProperty]
    public int Version { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
        await LoadAsync(id, cancellationToken) ? Page() : NotFound();

    public Task<IActionResult> OnPostAcceptAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Accept), "Request accepted.", cancellationToken);

    public Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Reject, Reason: RejectionReason), "Request rejected.", cancellationToken);

    private async Task<IActionResult> ApplyAsync(
        Guid id, LifecycleCommand command, string successMessage, CancellationToken cancellationToken)
    {
        var actor = User.ToActor();
        var result = await lifecycle.ApplyAsync(id, Version, command, actor, cancellationToken);

        switch (result.Outcome)
        {
            case LifecycleServiceOutcome.Applied:
                TempData["Flash"] = successMessage;
                // The change can take the request out of this user's view (e.g. staff rejecting it),
                // so go back to the work queue rather than to a page that would now be a 404.
                return await queries.GetDetailAsync(id, actor, cancellationToken) is null
                    ? RedirectToPage("/Staff/Index")
                    : RedirectToPage(new { id });

            case LifecycleServiceOutcome.NotFound:
                return NotFound();

            case LifecycleServiceOutcome.ValidationFailed when command.Event == LifecycleEvent.Reject:
                // Keep the page and the typed text so the reviewer can fix it
                ModelState.AddModelError(nameof(RejectionReason), result.Message ?? "Enter a reason.");
                return await LoadAsync(id, cancellationToken) ? Page() : NotFound();

            default:
                // Forbidden, InvalidTransition, Conflict: show why and the current state
                TempData["Error"] = result.Message;
                return RedirectToPage(new { id });
        }
    }

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToActor();
        var request = await queries.GetDetailAsync(id, actor, cancellationToken);
        if (request is null)
            return false;

        Detail = request;
        Version = request.Version;
        ShowRequester = request.RequesterId != actor.UserId;

        var state = request.ToLifecycleState();
        CanAccept = IsOffered(actor, state, LifecycleEvent.Accept);
        CanReject = IsOffered(actor, state, LifecycleEvent.Reject);
        return true;
    }

    private static bool IsOffered(Actor actor, RequestState state, LifecycleEvent lifecycleEvent) =>
        LifecycleTable.TryGetNext(state.Status, lifecycleEvent, out _)
        && LifecycleAuthorisation.IsAllowed(actor, state, new LifecycleCommand(lifecycleEvent));
}
