namespace SportsCenterAPI.Helpers;

public static class UserRoles
{
    public const string Member = "Member";
    public const string Receptionist = "Receptionist";
    public const string Coach = "Coach";
    public const string Manager = "Manager";
    public const string FrontDesk = Manager + "," + Receptionist;

    public static bool IsStaff(string role) => role is Manager or Receptionist or Coach;
}
