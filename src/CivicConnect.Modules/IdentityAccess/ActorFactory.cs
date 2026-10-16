namespace CivicConnect.Modules.IdentityAccess;

/// <summary>
/// Builds the <see cref="Actor"/> the rules work with from the role names held by ASP.NET Core Identity.
/// Role names in the database are exactly the <see cref="Role"/> enum names; unknown names are ignored.
/// </summary>
public static class ActorFactory
{
    public static Actor FromRoleNames(Guid userId, IEnumerable<string> roleNames)
    {
        var roles = new HashSet<Role>();
        foreach (var name in roleNames)
        {
            if (Enum.TryParse<Role>(name, ignoreCase: false, out var role) && Enum.IsDefined(role))
                roles.Add(role);
        }

        // Assumption until the team confirms it: supervisors hold the close permission (T6, FR-013).
        return new Actor(userId, roles, HasClosePermission: roles.Contains(Role.Supervisor));
    }
}
