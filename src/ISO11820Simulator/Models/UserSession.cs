namespace ISO11820Simulator.Models;

public sealed record UserSession(string UserId, string Username, string UserType)
{
    public bool IsAdmin => string.Equals(UserType, "admin", StringComparison.OrdinalIgnoreCase);
    public string RoleDisplay => IsAdmin ? "管理员" : "试验员";
}
