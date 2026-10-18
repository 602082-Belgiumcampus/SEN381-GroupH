using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Domain.Tests;

/// Which actions the request page offers each user (PED Section 9.3, state-dependent actions).
/// Combines the transition table (ADR-006) with the "who may do it" rules (ADR-007).
public class LifecycleActionsTests
{
    private static readonly Guid StaffAId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid StaffBId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid SupervisorId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid RequesterId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private static Actor Staff(Guid id) => new(id, new HashSet<Role> { Role.ServiceStaff });
    private static Actor Supervisor() => new(SupervisorId, new HashSet<Role> { Role.Supervisor }, HasClosePermission: true);
    private static Actor Requester() => new(RequesterId, new HashSet<Role> { Role.Requester });

    private static RequestState State(RequestStatus status, Guid? assignee = null) => new(status, assignee, Version: 3);

    [Fact]
    public void Submitted_request_offers_staff_accept_and_reject_only()
    {
        var actions = LifecycleActions.Available(Staff(StaffAId), State(RequestStatus.Submitted));

        Assert.Equal(new[] { LifecycleEvent.Accept, LifecycleEvent.Reject }, actions.Order());
    }

    [Fact]
    public void Accepted_request_offers_assign_to_supervisor_and_to_staff()
    {
        Assert.Equal(new[] { LifecycleEvent.Assign }, LifecycleActions.Available(Supervisor(), State(RequestStatus.Accepted)));
        Assert.Equal(new[] { LifecycleEvent.Assign }, LifecycleActions.Available(Staff(StaffAId), State(RequestStatus.Accepted)));
    }

    [Fact]
    public void Only_supervisors_may_assign_other_people()
    {
        Assert.True(LifecycleActions.CanAssignOthers(Supervisor()));
        Assert.False(LifecycleActions.CanAssignOthers(Staff(StaffAId)));
    }

    [Fact]
    public void Assigned_request_offers_start_to_the_assignee_only()
    {
        var assignedToA = State(RequestStatus.Assigned, StaffAId);

        Assert.Equal(new[] { LifecycleEvent.Start }, LifecycleActions.Available(Staff(StaffAId), assignedToA));
        Assert.Empty(LifecycleActions.Available(Staff(StaffBId), assignedToA));
        Assert.Empty(LifecycleActions.Available(Supervisor(), assignedToA));
    }

    [Fact]
    public void In_progress_request_offers_resolve_to_the_assignee_only()
    {
        var inProgress = State(RequestStatus.InProgress, StaffAId);

        Assert.Equal(new[] { LifecycleEvent.Resolve }, LifecycleActions.Available(Staff(StaffAId), inProgress));
        Assert.Empty(LifecycleActions.Available(Staff(StaffBId), inProgress));
    }

    [Fact]
    public void Resolved_request_offers_close_only_to_users_with_close_permission()
    {
        var resolved = State(RequestStatus.Resolved, StaffAId);

        Assert.Equal(new[] { LifecycleEvent.Close }, LifecycleActions.Available(Supervisor(), resolved));
        Assert.Empty(LifecycleActions.Available(Staff(StaffAId), resolved));
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Closed)]
    public void Terminal_states_offer_nothing_to_anyone(RequestStatus terminal)
    {
        Assert.Empty(LifecycleActions.Available(Supervisor(), State(terminal, StaffAId)));
        Assert.Empty(LifecycleActions.Available(Staff(StaffAId), State(terminal, StaffAId)));
    }

    [Theory]
    [InlineData(RequestStatus.Submitted)]
    [InlineData(RequestStatus.Accepted)]
    [InlineData(RequestStatus.Resolved)]
    public void Requester_is_never_offered_a_lifecycle_action(RequestStatus status)
    {
        Assert.Empty(LifecycleActions.Available(Requester(), State(status, RequesterId)));
    }

    [Fact]
    public void Full_journey_from_Submitted_to_Closed_through_the_engine()
    {
        var now = new DateTimeOffset(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);
        var state = new RequestState(RequestStatus.Submitted);

        state = Step(state, new(LifecycleEvent.Accept), Staff(StaffAId), now);
        state = Step(state, new(LifecycleEvent.Assign, TargetAssigneeId: StaffAId), Supervisor(), now);
        state = Step(state, new(LifecycleEvent.Start), Staff(StaffAId), now);
        state = Step(state, new(LifecycleEvent.Resolve, Note: "Replaced the bulb."), Staff(StaffAId), now);
        state = Step(state, new(LifecycleEvent.Close), Supervisor(), now);

        Assert.Equal(RequestStatus.Closed, state.Status);
        Assert.Equal(StaffAId, state.AssignedToId);
        Assert.Equal("Replaced the bulb.", state.ResolutionNote);
        Assert.Equal(now, state.ClosedAt);
        Assert.Equal(6, state.Version);
    }

    private static RequestState Step(RequestState state, LifecycleCommand command, Actor actor, DateTimeOffset now)
    {
        var result = LifecycleEngine.Apply(state, command, actor, now);
        Assert.True(result.Succeeded);
        return result.State;
    }
}
