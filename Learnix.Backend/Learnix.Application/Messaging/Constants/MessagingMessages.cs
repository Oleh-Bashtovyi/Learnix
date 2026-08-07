namespace Learnix.Application.Messaging.Constants;

public static class MessagingMessages
{
    public static string ConversationNotFound => "Conversation not found.";
    public static string NotAParticipant => "You are not a participant of this conversation.";
    public static string SenderNotFound => "Sender not found.";
    public static string MustBeEnrolledToMessage => "You must be enrolled in this course to send messages.";
    public static string ConversationBlocked => "This conversation is blocked.";
    public static string AlreadyBlocked => "This conversation is already blocked.";
    public static string NotBlocked => "This conversation is not blocked.";
    public static string OnlyBlockerCanUnblock => "Only the user who blocked this conversation can unblock it.";
}
