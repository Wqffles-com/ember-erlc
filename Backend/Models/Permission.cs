namespace Backend.Models;

[Flags]
public enum Permission : long
{
    None = 0,

    // General
    ViewServer = 1L << 0,
    ManageServer = 1L << 1,

    // Moderation
    KickMembers = 1L << 2,
    BanMembers = 1L << 3,
    MuteMembers = 1L << 4,
    WarnMembers = 1L << 5,
    ViewModLogs = 1L << 6,
    ManageModLogs = 1L << 7,

    // Roles
    ManageRoles = 1L << 8,
    AssignRoles = 1L << 9,

    // Members
    ViewMembers = 1L << 10,
    ManageMembers = 1L << 11,

    // Applications
    ViewApplications = 1L << 12,
    ManageApplications = 1L << 13,

    // CAD
    ViewCad = 1L << 14,
    ManageCad = 1L << 15,

    // Activity
    ViewActivity = 1L << 16,
    ManageActivity = 1L << 17,

    // ModPanel
    UseModPanel = 1L << 18,

    // Admin
    Administrator = 1L << 63
}

public static class PermissionExtensions
{
    public static bool HasPermission(this long permissions, Permission permission)
    {
        if ((permissions & (long)Permission.Administrator) != 0)
            return true;

        if (permission == Permission.None)
            return true;

        return (permissions & (long)permission) != 0;
    }

    public static long AddPermission(this long permissions, Permission permission)
    {
        return permissions | (long)permission;
    }

    public static long RemovePermission(this long permissions, Permission permission)
    {
        return permissions & ~(long)permission;
    }

    public static bool HasAllPermissions(this long permissions, params Permission[] required)
    {
        if ((permissions & (long)Permission.Administrator) != 0)
            return true;

        foreach (var perm in required)
        {
            if ((permissions & (long)perm) == 0)
                return false;
        }

        return true;
    }

    public static bool HasAnyPermissions(this long permissions, params Permission[] required)
    {
        if ((permissions & (long)Permission.Administrator) != 0)
            return true;

        foreach (var perm in required)
        {
            if ((permissions & (long)perm) != 0)
                return true;
        }

        return false;
    }

    public static List<Permission> GetAllPermissions(this long permissions)
    {
        var result = new List<Permission>();

        if ((permissions & (long)Permission.Administrator) != 0)
        {
            result.Add(Permission.Administrator);
            return result;
        }

        foreach (Permission value in Enum.GetValues<Permission>())
        {
            if (value == Permission.None || value == Permission.Administrator)
                continue;

            if ((permissions & (long)value) != 0)
                result.Add(value);
        }

        return result;
    }
}
