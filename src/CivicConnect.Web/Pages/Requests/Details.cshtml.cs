using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestQueries;
using CivicConnect.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CivicConnect.Web.Pages.Requests;

/// <summary>
/// Request detail for requesters and staff (FR-009) with every lifecycle action:
/// Accept (T1), Reject (T2), Assign (T3), Start (T4), Resolve (T5), Close (T6) and Reopen (T7),
/// plus recording actions in the work log (FR-012).
/// The page only offers actions <see cref="LifecycleActions"/> allows for this user and state,
/// but every post goes through <see cref="ILifecycleService"/>, which checks again (NFR-002).
/// </summary>
public class DetailsModel(IRequestQueries queries, ILifecycleService lifecycle) : PageModel
{
    public RequestDetail Detail { get; private set; } = null!;

    public IReadOnlySet<LifecycleEvent> Actions { get; private set; } = new HashSet<LifecycleEvent>();

    public bool CanAct(LifecycleEvent lifecycleEvent) => Actions.Contains(lifecycleEvent);

    /// <summary>FR-012: the assignee or a supervisor, while the work is open.</summary>
    public bool CanRecordAction { get; private set; }

    /// <summary>Supervisors choose who to assign; service staff can only take the request themselves.</summary>
    public bool CanAssignOthers { get; private set; }

    public IReadOnlyList<SelectListItem> StaffOptions { get; private set; } = Array.Empty<SelectListItem>();

    /// <summary>Staff see the requester's name; requesters already know it is theirs.</summary>
    public bool ShowRequester { get; private set; }

    [BindProperty]
    public int Version { get; set; }

    [BindProperty]
    public string? RejectionReason { get; set; }

    [BindProperty]
    public Guid? AssigneeId { get; set; }

    [BindProperty]
    public string? ResolutionNote { get; set; }

    [BindProperty]
    public string? ReopenReason { get; set; }

    [BindProperty]
    public string? ActionText { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken) =>
        await LoadAsync(id, cancellationToken) ? Page() : NotFound();

    public Task<IActionResult> OnPostAcceptAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Accept), "Request accepted.", cancellationToken);

    public Task<IActionResult> OnPostRejectAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Reject, Reason: RejectionReason),
            "Request rejected.", cancellationToken);

    public Task<IActionResult> OnPostAssignAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Assign, TargetAssigneeId: AssigneeId),
            "Request assigned.", cancellationToken);

    public Task<IActionResult> OnPostAssignToMeAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Assign, TargetAssigneeId: User.GetUserId()),
            "The request is now assigned to you.", cancellationToken);

    public Task<IActionResult> OnPostStartAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Start), "Work started. The request is now in progress.", cancellationToken);

    public Task<IActionResult> OnPostResolveAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Resolve, Note: ResolutionNote),
            "Request resolved.", cancellationToken);

    public Task<IActionResult> OnPostCloseAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Close), "Request closed.", cancellationToken);

    public Task<IActionResult> OnPostReopenAsync(Guid id, CancellationToken cancellationToken) =>
        ApplyAsync(id, new LifecycleCommand(LifecycleEvent.Reopen, Reason: ReopenReason),
            "Request reopened. It is back in progress with the same staff member.", cancellationToken);

    public async Task<IActionResult> OnPostRecordActionAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await lifecycle.RecordActionAsync(id, ActionText, User.ToActor(), cancellationToken);
        return await HandleAsync(id, result, nameof(ActionText), "Action recorded.", cancellationToken);
    }

    private async Task<IActionResult> ApplyAsync(
        Guid id, LifecycleCommand command, string successMessage, CancellationToken cancellationToken)
    {
        var result = await lifecycle.ApplyAsync(id, Version, command, User.ToActor(), cancellationToken);
        return await HandleAsync(id, result, FieldFor(command.Event), successMessage, cancellationToken);
    }

    private async Task<IActionResult> HandleAsync(
        Guid id, LifecycleServiceResult result, string field, string successMessage, CancellationToken cancellationToken)
    {
        switch (result.Outcome)
        {
            case LifecycleServiceOutcome.Applied:
                TempData["Flash"] = successMessage;
                // The change can take the request out of this user's view (e.g. staff rejecting it),
                // so go back to the work queue rather than to a page that would now be a 404.
                return await queries.GetDetailAsync(id, User.ToActor(), cancellationToken) is null
                    ? RedirectToPage("/Staff/Index")
                    : RedirectToPage(new { id });

            case LifecycleServiceOutcome.NotFound:
                return NotFound();

            case LifecycleServiceOutcome.ValidationFailed:
                // Keep the page and anything typed so the user can fix it beside the right field
                ModelState.AddModelError(field, result.Message ?? "Check the details and try again.");
                return await LoadAsync(id, cancellationToken) ? Page() : NotFound();

            default:
                // Forbidden, InvalidTransition, Conflict: show why, with the current state
                TempData["Error"] = result.Message;
                return RedirectToPage(new { id });
        }
    }

    private static string FieldFor(LifecycleEvent lifecycleEvent) => lifecycleEvent switch
    {
        LifecycleEvent.Reject => nameof(RejectionReason),
        LifecycleEvent.Resolve => nameof(ResolutionNote),
        LifecycleEvent.Assign => nameof(AssigneeId),
        LifecycleEvent.Reopen => nameof(ReopenReason),
        _ => string.Empty
    };

    private async Task<bool> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var actor = User.ToActor();
        var request = await queries.GetDetailAsync(id, actor, cancellationToken);
        if (request is null)
            return false;

        Detail = request;
        Version = request.Version;
        ShowRequester = request.RequesterId != actor.UserId;
        Actions = LifecycleActions.Available(actor, request.ToLifecycleState());
        CanRecordAction = RequestActionRules.CanRecord(actor, request.ToLifecycleState());
        CanAssignOthers = CanAct(LifecycleEvent.Assign) && LifecycleActions.CanAssignOthers(actor);

        if (CanAssignOthers)
        {
            var staff = await queries.GetAssignableStaffAsync(cancellationToken);
            StaffOptions = staff
                .Select(s => new SelectListItem(s.Name, s.Id.ToString(), s.Id == AssigneeId))
                .ToList();
        }

        return true;
    }
}
