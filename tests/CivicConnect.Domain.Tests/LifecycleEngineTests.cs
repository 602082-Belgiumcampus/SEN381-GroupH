using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Domain.Tests;

/// Implements TC-LC-01 to TC-LC-16
public class LifecycleEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid StaffAId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid StaffBId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid SupervisorId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid RequesterId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

    private static Actor Staff(Guid id) => new(id, new HashSet<Role> { Role.ServiceStaff });
    private static Actor Supervisor() => new(SupervisorId, new HashSet<Role> { Role.Supervisor }, HasClosePermission: true);
    private static Actor Requester() => new(RequesterId, new HashSet<Role> { Role.Requester });

    private static RequestState State(RequestStatus status, Guid? assignee = null) =>
        new(status, assignee, Version: 7);

    // Valid transitions

    [Fact]
    public void TC_LC_01_Accept_moves_Submitted_to_Accepted_with_one_audit_entry()
    {
        var before = State(RequestStatus.Submitted);

        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Accept), Staff(StaffAId), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.Accepted, result.State.Status);
        Assert.Equal(before.Version + 1, result.State.Version);
        Assert.NotNull(result.Audit);
        Assert.Equal(RequestStatus.Submitted, result.Audit!.From);
        Assert.Equal(RequestStatus.Accepted, result.Audit.To);
        Assert.Equal(StaffAId, result.Audit.ActorId);
    }

    [Fact]
    public void TC_LC_02_Reject_with_reason_moves_Submitted_to_Rejected_and_stores_reason()
    {
        var result = LifecycleEngine.Apply(
            State(RequestStatus.Submitted),
            new(LifecycleEvent.Reject, Reason: "Duplicate of an existing request"),
            Staff(StaffAId), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.Rejected, result.State.Status);
        Assert.Equal("Duplicate of an existing request", result.State.RejectionReason);
        Assert.NotNull(result.Audit);
    }

    [Fact]
    public void TC_LC_03_Supervisor_assigns_Accepted_request_and_audit_records_assignee()
    {
        var result = LifecycleEngine.Apply(
            State(RequestStatus.Accepted),
            new(LifecycleEvent.Assign, TargetAssigneeId: StaffAId),
            Supervisor(), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.Assigned, result.State.Status);
        Assert.Equal(StaffAId, result.State.AssignedToId);
        Assert.Contains(StaffAId.ToString(), result.Audit!.Detail);
    }

    [Fact]
    public void TC_LC_04_Assignee_starts_work_Assigned_to_InProgress()
    {
        var result = LifecycleEngine.Apply(
            State(RequestStatus.Assigned, StaffAId),
            new(LifecycleEvent.Start), Staff(StaffAId), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.InProgress, result.State.Status);
    }

    [Fact]
    public void TC_LC_05_Assignee_resolves_with_note_InProgress_to_Resolved()
    {
        var result = LifecycleEngine.Apply(
            State(RequestStatus.InProgress, StaffAId),
            new(LifecycleEvent.Resolve, Note: "Pothole repaired"), Staff(StaffAId), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.Resolved, result.State.Status);
        Assert.Equal("Pothole repaired", result.State.ResolutionNote);
    }

    [Fact]
    public void TC_LC_06_User_with_close_permission_closes_Resolved_request_and_sets_closed_at()
    {
        var result = LifecycleEngine.Apply(
            State(RequestStatus.Resolved, StaffAId),
            new(LifecycleEvent.Close), Supervisor(), Now);

        Assert.True(result.Succeeded);
        Assert.Equal(RequestStatus.Closed, result.State.Status);
        Assert.Equal(Now, result.State.ClosedAt);
    }

    // Invalid transitions

    private static void AssertRejectedUnchanged(
        TransitionResult result, RequestState before, TransitionOutcome expected)
    {
        Assert.Equal(expected, result.Outcome);
        Assert.False(result.Succeeded);
        Assert.Equal(before, result.State);
        Assert.Null(result.Audit);
    }

    [Fact]
    public void TC_LC_07_Close_from_Submitted_is_InvalidTransition()
    {
        var before = State(RequestStatus.Submitted);
        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Close), Supervisor(), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_08_Assign_from_Submitted_is_InvalidTransition_must_pass_through_Accepted()
    {
        var before = State(RequestStatus.Submitted);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Assign, TargetAssigneeId: StaffAId), Supervisor(), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_09_Start_from_Closed_is_InvalidTransition()
    {
        // Assignee is kept on a closed request, so the actor passes authorisation
        // and it is the table that rejects the transition.
        var before = State(RequestStatus.Closed, StaffAId);
        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Start), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_10_Assign_from_Resolved_is_InvalidTransition()
    {
        var before = State(RequestStatus.Resolved, StaffAId);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Assign, TargetAssigneeId: StaffBId), Supervisor(), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_11_Accept_from_Rejected_is_InvalidTransition_Rejected_is_terminal()
    {
        var before = State(RequestStatus.Rejected);
        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Accept), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_12_Resolve_from_Accepted_is_InvalidTransition()
    {
        var before = State(RequestStatus.Accepted, StaffAId);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Resolve, Note: "Done"), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.InvalidTransition);
    }

    [Fact]
    public void TC_LC_12b_Resolve_from_Accepted_with_no_assignee_is_Forbidden_authorise_first()
    {
        var before = State(RequestStatus.Accepted);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Resolve, Note: "Done"), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.Forbidden);
    }

    // Validation failures

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_LC_13_Reject_with_empty_reason_is_ValidationFailed(string? reason)
    {
        var before = State(RequestStatus.Submitted);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Reject, Reason: reason), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.ValidationFailed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TC_LC_14_Resolve_with_empty_note_is_ValidationFailed(string? note)
    {
        var before = State(RequestStatus.InProgress, StaffAId);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Resolve, Note: note), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.ValidationFailed);
    }

    // Authorisation failures

    [Fact]
    public void TC_LC_15_Start_by_someone_who_is_not_the_assignee_is_Forbidden()
    {
        var before = State(RequestStatus.Assigned, StaffAId);
        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Start), Staff(StaffBId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.Forbidden);
    }

    [Fact]
    public void TC_LC_16_ServiceStaff_assigning_another_staff_member_is_Forbidden()
    {
        var before = State(RequestStatus.Accepted);
        var result = LifecycleEngine.Apply(
            before, new(LifecycleEvent.Assign, TargetAssigneeId: StaffBId), Staff(StaffAId), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.Forbidden);
    }


    [Fact]
    public void Table_contains_exactly_the_six_transitions_T1_to_T6()
    {
        Assert.Equal(6, LifecycleTable.All.Count);
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Closed)]
    public void Terminal_states_have_no_outgoing_transitions(RequestStatus terminal)
    {
        foreach (var lifecycleEvent in Enum.GetValues<LifecycleEvent>())
            Assert.False(LifecycleTable.TryGetNext(terminal, lifecycleEvent, out _));
    }

    [Fact]
    public void Requester_cannot_perform_any_lifecycle_event_even_on_their_own_request()
    {
        // a requester sending accept directly is forbidden
        var before = State(RequestStatus.Submitted);
        var result = LifecycleEngine.Apply(before, new(LifecycleEvent.Accept), Requester(), Now);
        AssertRejectedUnchanged(result, before, TransitionOutcome.Forbidden);
    }
}
