export interface ConversationSummary {
    id: string;
    courseId: string;
    courseName: string;
    otherUserId: string;
    otherUserName: string;
    otherUserAvatarPath: string | null;
    lastMessagePreview: string | null;
    lastMessageAt: string | null;
    unreadCount: number;
    /** Only an instructor has a public profile to link to — a student does not, by design. */
    otherUserIsInstructor: boolean;
    isBlocked: boolean;
    /** Whether the current user is the one who blocked — only they can unblock. */
    blockedByMe: boolean;
}

export interface ConversationDetail {
    id: string;
    courseId: string;
    courseName: string;
    otherUserId: string;
    otherUserName: string;
    otherUserAvatarPath: string | null;
    unreadCount: number;
    isBlocked: boolean;
    blockedByMe: boolean;
}

export interface MessageItem {
    id: string;
    senderId: string;
    senderName: string;
    senderAvatarPath: string | null;
    content: string;
    sentAt: string;
    isFromCurrentUser: boolean;
}

export interface UnreadCount {
    totalUnread: number;
}

export interface StartConversationRequest {
    courseId: string;
}

export interface SendMessageRequest {
    content: string;
}

export interface NewMessageNotification {
    conversationId: string;
    messageId: string;
    senderId: string;
    senderName: string;
    senderAvatarPath: string | null;
    content: string;
    sentAt: string;
}

export interface UnreadCountNotification {
    totalUnread: number;
}
