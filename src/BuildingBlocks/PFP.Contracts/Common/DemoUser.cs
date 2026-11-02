namespace PFP.Contracts.Common;

/// <summary>
/// Fixed user id used by every service until the Auth service and JWT propagation are implemented.
/// All seeded demo data and unauthenticated requests resolve to this id.
/// </summary>
public static class DemoUser
{
    public static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
}
