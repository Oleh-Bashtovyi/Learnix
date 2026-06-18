namespace Learnix.Application.Users.Constants;

public static class UserMessages
{
    public static string OnlyAdminsCanChangeRoles => "Only admins can change user roles.";
    public static string UserNotFound(Guid userId) => $"User {userId} not found.";
    public static string GenericUserNotFound => "User not found.";
    public static string UserAlreadyHasRole(string role) => $"User already has the '{role}' role.";
    public static string UserDoesNotHaveRole(string role) => $"User does not have the '{role}' role.";
    public static string OnlyAdminsCanBanUsers => "Only admins can ban users.";
    public static string AdminsCannotBanThemselves => "Admins cannot ban themselves.";
    public static string UserIsAlreadyBanned => "User is already banned.";
    public static string OnlyAdminsCanUnbanUsers => "Only admins can unban users.";
    public static string UserIsNotBanned => "User is not banned.";
    public static string OnlyAdminsCanDeleteUsers => "Only admins can delete users.";
    public static string AdminsCannotDeleteThemselves => "Admins cannot delete themselves.";
    public static string UserIsAlreadyDeleted => "User is already deleted.";
    public static string OnlyAdminsCanRecoverUsers => "Only admins can recover users.";
    public static string UserIsNotDeleted => "User is not deleted.";
    public static string CannotRemoveOwnAdminRole => "Cannot remove your own admin role.";
    public static string CannotRemoveLastAdmin => "Cannot remove the last administrator from the system.";
    public static string OnlyAdminsCanListUsers => "Only admins can list users.";
}
