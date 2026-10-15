using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;

namespace CivicConnect.Domain.Tests;

/// Pure parts of TC-AZ-01 and TC-AZ-05 (who may see a request, AS-05), plus the role mapping.
public class RequestVisibilityTests
{
    private static readonly Guid RequesterA = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid RequesterB = Guid.Parse("00000000-0000-0000-0000-0000000000c2");
    private static readonly Guid StaffA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid StaffB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid SupervisorId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    private static Actor With(Guid id, params Role[] roles) => new(id, roles.ToHashSet());

    [Fact]
    public void TC_AZ_01_Requester_cannot_see_another_requesters_request()
    {
        var requestOfA = new RequestAccessFacts(RequesterA, null, RequestStatus.Submitted);

        Assert.True(RequestVisibility.CanView(With(RequesterA, Role.Requester), requestOfA));
        Assert.False(RequestVisibility.CanView(With(RequesterB, Role.Requester), requestOfA));
    }

    [Theory]
    [InlineData(RequestStatus.Submitted, true)]
    [InlineData(RequestStatus.Accepted, true)]
    [InlineData(RequestStatus.Rejected, false)]
    [InlineData(RequestStatus.Closed, false)]
    public void TC_AZ_05a_Staff_see_unassigned_Submitted_and_Accepted_requests_only(RequestStatus status, bool visible)
    {
        var unassigned = new RequestAccessFacts(RequesterA, null, status);

        Assert.Equal(visible, RequestVisibility.CanView(With(StaffB, Role.ServiceStaff), unassigned));
    }

    [Fact]
    public void TC_AZ_05b_Assigned_request_is_visible_to_its_assignee_but_not_other_staff()
    {
        var assignedToA = new RequestAccessFacts(RequesterA, StaffA, RequestStatus.InProgress);

        Assert.True(RequestVisibility.CanView(With(StaffA, Role.ServiceStaff), assignedToA));
        Assert.False(RequestVisibility.CanView(With(StaffB, Role.ServiceStaff), assignedToA));
    }

    [Theory]
    [InlineData(Role.Supervisor)]
    [InlineData(Role.Management)]
    public void TC_AZ_05c_Supervisors_and_management_see_all_requests(Role role)
    {
        var assignedToA = new RequestAccessFacts(RequesterA, StaffA, RequestStatus.Closed);

        Assert.True(RequestVisibility.CanView(With(SupervisorId, role), assignedToA));
    }

    [Fact]
    public void Administrator_role_alone_does_not_grant_request_access()
    {
        var request = new RequestAccessFacts(RequesterA, null, RequestStatus.Submitted);

        Assert.False(RequestVisibility.CanView(With(SupervisorId, Role.Administrator), request));
    }

    [Fact]
    public void ActorFactory_maps_role_names_and_ignores_unknown_ones()
    {
        var actor = ActorFactory.FromRoleNames(StaffA, new[] { "ServiceStaff", "NotARole", "supervisor" });

        Assert.True(actor.IsInRole(Role.ServiceStaff));
        Assert.False(actor.IsInRole(Role.Supervisor)); // names are case-sensitive, as stored by Identity
        Assert.False(actor.HasClosePermission);
    }

    [Fact]
    public void ActorFactory_gives_supervisors_the_close_permission()
    {
        Assert.True(ActorFactory.FromRoleNames(SupervisorId, new[] { "Supervisor" }).HasClosePermission);
    }
}
