using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CivicConnect.Web.Pages;

/// <summary>
/// Read-only view of the lifecycle table plus a mock request that can be walked through the states
/// The rows come from LifecycleTable itself and the mock request is moved by LifecycleEngine
/// </summary>
public class LifecycleModel : PageModel
{
    public sealed record TransitionRow(string Id, RequestStatus From, LifecycleEvent Event, RequestStatus To);

    public sealed record MockAction(LifecycleEvent Event, bool Allowed, string Steps);

    // The mock user is permitted to do everything, so the mock shows the table's rules
    // and not the "who may do it" rules.
    private static readonly Actor MockActor = new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        new HashSet<Role> { Role.Supervisor },
        HasClosePermission: true);

    [BindProperty(SupportsGet = true)]
    public string? Steps { get; set; }

    public IReadOnlyList<TransitionRow> Rows { get; private set; } = Array.Empty<TransitionRow>();

    public RequestState Current { get; private set; } = new(RequestStatus.Submitted);

    public IReadOnlyList<AuditRecord> History { get; private set; } = Array.Empty<AuditRecord>();

    public IReadOnlyList<MockAction> Actions { get; private set; } = Array.Empty<MockAction>();

    public string? Refusal { get; private set; }

    public bool IsFinal => Actions.All(a => !a.Allowed);

    public bool HasPassed(RequestStatus status) => History.Any(a => a.From == status);

    public static string Label(RequestStatus status) =>
        status == RequestStatus.InProgress ? "In progress" : status.ToString();

    public void OnGet()
    {
        Rows = LifecycleTable.All
            .OrderBy(kv => kv.Key.From)
            .ThenBy(kv => kv.Key.Event)
            .Select((kv, index) => new TransitionRow($"T{index + 1}", kv.Key.From, kv.Key.Event, kv.Value))
            .ToList();

        ReplayMockRequest();
    }

    private void ReplayMockRequest()
    {
        var state = new RequestState(RequestStatus.Submitted);
        var applied = new List<LifecycleEvent>();
        var history = new List<AuditRecord>();

        foreach (var name in (Steps ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<LifecycleEvent>(name, ignoreCase: true, out var lifecycleEvent) || !Enum.IsDefined(lifecycleEvent))
            {
                Refusal = $"\"{name}\" is not a lifecycle action.";
                break;
            }

            var result = LifecycleEngine.Apply(state, MockCommand(lifecycleEvent), MockActor, DateTimeOffset.UtcNow);
            if (!result.Succeeded)
            {
                Refusal = result.Message;
                break;
            }

            state = result.State;
            applied.Add(lifecycleEvent);
            history.Add(result.Audit!);
        }

        Current = state;
        History = history;
        Actions = Enum.GetValues<LifecycleEvent>()
            .Select(e => new MockAction(
                e,
                LifecycleTable.TryGetNext(state.Status, e, out _),
                string.Join(',', applied.Append(e))))
            .ToList();
    }

    private static LifecycleCommand MockCommand(LifecycleEvent lifecycleEvent) =>
        new(lifecycleEvent, MockActor.UserId, "Mock rejection reason", "Mock resolution note");
}
