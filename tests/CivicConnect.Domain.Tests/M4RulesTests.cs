using CivicConnect.Modules.Administration;
using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.Reporting;
using CivicConnect.Modules.RequestIntake;

namespace CivicConnect.Domain.Tests;

/// Pure rules added in M4: overdue (TC-RP-01, CR-001), reports, record action (FR-012),
/// feedback messages (FR-006, CL-02), log on behalf (FR-020) and administration (FR-021, FR-022).
public class M4RulesTests
{
    private static readonly DateTimeOffset Created = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid StaffAId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid StaffBId = Guid.Parse("00000000-0000-0000-0000-00000000000b");

    private static Actor With(Guid id, params Role[] roles) => new(id, roles.ToHashSet());

    // --- Overdue (TC-RP-01: boundary at the target period) -----------------------------------

    [Fact]
    public void TC_RP_01a_Exactly_at_the_target_is_not_overdue()
    {
        Assert.False(OverdueRule.IsOverdue(RequestStatus.InProgress, Created, 10, Created.AddDays(10)));
    }

    [Fact]
    public void TC_RP_01b_One_second_past_the_target_is_overdue()
    {
        Assert.True(OverdueRule.IsOverdue(RequestStatus.InProgress, Created, 10, Created.AddDays(10).AddSeconds(1)));
    }

    [Theory]
    [InlineData(RequestStatus.Resolved)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Rejected)]
    public void TC_RP_01c_Finished_requests_are_never_overdue(RequestStatus status)
    {
        Assert.False(OverdueRule.IsOverdue(status, Created, 1, Created.AddDays(100)));
    }

    [Fact]
    public void TC_RP_01d_A_changed_target_period_changes_the_result()
    {
        var now = Created.AddDays(7);
        Assert.True(OverdueRule.IsOverdue(RequestStatus.Submitted, Created, 5, now));
        Assert.False(OverdueRule.IsOverdue(RequestStatus.Submitted, Created, 14, now));
    }

    [Fact]
    public void Days_overdue_counts_a_part_day_as_a_full_day()
    {
        var dueAt = Created.AddDays(5);
        Assert.Equal(0, ReportCalculator.DaysOverdue(dueAt, dueAt));
        Assert.Equal(1, ReportCalculator.DaysOverdue(dueAt, dueAt.AddHours(3)));
        Assert.Equal(2, ReportCalculator.DaysOverdue(dueAt, dueAt.AddDays(1).AddHours(1)));
    }

    // --- Report arithmetic (FR-014, FR-015) ---------------------------------------------------

    [Fact]
    public void Summary_counts_totals_open_overdue_and_average_time_to_resolve()
    {
        var now = Created.AddDays(20);
        var rows = new List<ReportRow>
        {
            new(RequestStatus.Submitted, "Cleaning", 5, Created, null),            // open, overdue
            new(RequestStatus.InProgress, "Cleaning", 5, now.AddDays(-1), null),   // open, not overdue
            new(RequestStatus.Resolved, "Facilities", 10, Created, Created.AddDays(2)),
            new(RequestStatus.Closed, "Facilities", 10, Created, Created.AddDays(4)),
            new(RequestStatus.Rejected, "Facilities", 10, Created, null)
        };

        var summary = ReportCalculator.Summarise(rows, now);

        Assert.Equal(5, summary.Total);
        Assert.Equal(2, summary.Open);
        Assert.Equal(1, summary.Overdue);
        Assert.Equal(3.0, summary.AverageDaysToResolve);
        Assert.Equal(1, summary.ByStatus.Single(s => s.Status == RequestStatus.Rejected).Count);

        var cleaning = summary.ByCategory.Single(c => c.Category == "Cleaning");
        Assert.Equal((2, 2, 1), (cleaning.Total, cleaning.Open, cleaning.Overdue));
        Assert.Null(cleaning.AverageDaysToResolve);
    }

    [Fact]
    public void Summary_of_no_requests_is_all_zero()
    {
        var summary = ReportCalculator.Summarise([], Created);
        Assert.Equal((0, 0, 0), (summary.Total, summary.Open, summary.Overdue));
        Assert.Null(summary.AverageDaysToResolve);
        Assert.Empty(summary.ByCategory);
    }

    // --- Record action (FR-012) ---------------------------------------------------------------

    [Theory]
    [InlineData(RequestStatus.Assigned, true)]
    [InlineData(RequestStatus.InProgress, true)]
    [InlineData(RequestStatus.Accepted, false)]
    [InlineData(RequestStatus.Resolved, false)]
    [InlineData(RequestStatus.Closed, false)]
    public void Assignee_records_actions_only_while_the_work_is_open(RequestStatus status, bool allowed)
    {
        var state = new RequestState(status, StaffAId);
        Assert.Equal(allowed, RequestActionRules.CanRecord(With(StaffAId, Role.ServiceStaff), state));
    }

    [Fact]
    public void Other_staff_cannot_record_actions_but_a_supervisor_can()
    {
        var state = new RequestState(RequestStatus.InProgress, StaffAId);
        Assert.False(RequestActionRules.CanRecord(With(StaffBId, Role.ServiceStaff), state));
        Assert.True(RequestActionRules.CanRecord(With(StaffBId, Role.Supervisor), state));
    }

    [Fact]
    public void Action_text_is_required_and_limited()
    {
        Assert.NotNull(RequestActionRules.Validate("   "));
        Assert.NotNull(RequestActionRules.Validate(new string('a', RequestActionRules.TextMaxLength + 1)));
        Assert.Null(RequestActionRules.Validate("Ordered a replacement part"));
    }

    // --- Feedback messages (FR-006, CL-02) ----------------------------------------------------

    [Theory]
    [InlineData(LifecycleEvent.Accept)]
    [InlineData(LifecycleEvent.Reject)]
    [InlineData(LifecycleEvent.Assign)]
    [InlineData(LifecycleEvent.Start)]
    [InlineData(LifecycleEvent.Resolve)]
    [InlineData(LifecycleEvent.Close)]
    [InlineData(LifecycleEvent.Reopen)]
    public void Every_lifecycle_event_produces_a_notice_naming_the_request(LifecycleEvent lifecycleEvent)
    {
        var message = FeedbackMessages.ForTransition(lifecycleEvent, "CC-2026-000042", "detail");
        Assert.NotNull(message);
        Assert.Contains("CC-2026-000042", message);
    }

    [Fact]
    public void Rejection_notice_includes_the_reason()
    {
        Assert.Contains("Not our responsibility", FeedbackMessages.ForTransition(LifecycleEvent.Reject, "CC-1", "Not our responsibility"));
    }

    [Fact]
    public void Notice_is_cut_to_the_column_length()
    {
        var message = FeedbackMessages.ForTransition(LifecycleEvent.Resolve, "CC-1", new string('x', 2000))!;
        Assert.Equal(FeedbackMessages.MessageMaxLength, message.Length);
    }

    // --- Log on behalf (FR-020) ---------------------------------------------------------------

    [Theory]
    [InlineData(Role.ServiceStaff, true)]
    [InlineData(Role.Supervisor, true)]
    [InlineData(Role.Requester, false)]
    [InlineData(Role.Management, false)]
    [InlineData(Role.Administrator, false)]
    public void Only_service_staff_and_supervisors_may_log_on_behalf(Role role, bool allowed)
    {
        Assert.Equal(allowed, OnBehalfRules.CanLogOnBehalf(With(StaffAId, role)));
    }

    [Fact]
    public void On_behalf_needs_an_email_a_channel_and_a_name_for_a_new_account()
    {
        var request = new SubmitRequestInput(Guid.NewGuid(), "Title", "Description", null);

        var errors = OnBehalfRules.Validate(new OnBehalfInput("not-an-email", null, "Carrier pigeon", request), nameNeeded: true);

        Assert.Equal(
            new[] { nameof(OnBehalfInput.RequesterEmail), nameof(OnBehalfInput.RequesterName), nameof(OnBehalfInput.ContactChannel) },
            errors.Select(e => e.Field));
        Assert.Empty(OnBehalfRules.Validate(new OnBehalfInput("thandi@example.org", null, ContactChannels.Phone, request), nameNeeded: false));
    }

    // --- Administration (FR-021, FR-022) ------------------------------------------------------

    [Theory]
    [InlineData(null, 5, "Name")]
    [InlineData("Roads", 0, "TargetResolutionDays")]
    [InlineData("Roads", 366, "TargetResolutionDays")]
    public void Category_needs_a_name_and_a_sensible_target(string? name, int days, string field)
    {
        Assert.Equal(field, Assert.Single(AdminRules.ValidateCategory(new CategoryInput(name, days))).Field);
    }

    [Fact]
    public void Administrator_cannot_lock_themselves_out()
    {
        var me = Guid.NewGuid();
        Assert.NotNull(AdminRules.CheckSelfLockout(me, me, null, newActive: false));
        Assert.NotNull(AdminRules.CheckSelfLockout(me, me, [Role.Supervisor], null));
        Assert.Null(AdminRules.CheckSelfLockout(me, me, [Role.Administrator, Role.Supervisor], null));
        Assert.Null(AdminRules.CheckSelfLockout(me, Guid.NewGuid(), null, newActive: false));
    }
}
