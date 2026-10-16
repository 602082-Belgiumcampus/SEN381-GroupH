namespace CivicConnect.Modules.IdentityAccess;

public enum Role
{
    Requester,
    ServiceStaff,
    Supervisor,
    Management,
    Administrator
}

public sealed record Actor(Guid UserId, IReadOnlySet<Role> Roles, bool HasClosePermission = false)
{
    public bool IsInRole(Role role) => Roles.Contains(role);
}
