namespace Learnix.Application.InstructorApplications.Constants;

public static class InstructorApplicationMessages
{
    public static string ApplicationNotFound => "Application not found.";
    public static string PendingApplicationExists => "You already have a pending application.";
    public static string OnlyPendingCanBeApproved => "Only pending applications can be approved.";
    public static string OnlyPendingCanBeRejected => "Only pending applications can be rejected.";
    public static string OnlyAdminsApprove => "Only admins can approve applications.";
    public static string OnlyAdminsReject => "Only admins can reject applications.";
    public static string AlreadyInstructor => "You are already an instructor.";
    public static string AdminsCannotSubmit => "Admins cannot submit applications. You can assign the Instructor role directly.";
    public static string ApplicationAlreadyApproved => "Your application has already been approved.";
    public static string OnlyAdminsViewPending => "Only admins can view pending applications.";
}
