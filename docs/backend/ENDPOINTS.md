# Learnix — API Surface

> **Generated from the controllers by `scripts/check-endpoints.mjs`.** CI fails if this file
> and `Learnix.API/Controllers/` disagree. Do not hand-edit method, path, auth or rate limit —
> change the controller. The **Description** column is prose: edit it freely, it is preserved
> across regeneration and never verified.

`Rate limit` values are the policies in `Learnix.API/RateLimiting/RateLimitPolicies.cs`;
`Default` means no `[EnableRateLimiting]` attribute — the global limiter applies.

**`Auth` is only what the attributes say.** Resource-level authorization lives in the handlers
(ADR-BACK-ARCH: authorization in handlers, not controllers) — `Authenticated` on a lesson or
section mutation still means *course owner or admin*, enforced by `course.IsOwnerOrAdmin(...)`.
Where that applies, the Description column says so.

The *decisions* behind these endpoints live in `docs/backend/decisions/`; this file is only
the surface they add up to.

## Achievements

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/achievements/me` | Authenticated | `Default` | Fetch user's achievements and progress counters |
| `POST` | `/api/achievements/{achievementId}/seen` | Authenticated | `Default` | Mark an achievement as seen (clears the 'new' badge) |

## Admin

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/admin/stats` | Admin | `Default` | Platform-wide dashboard counters (users, courses, revenue) |
| `GET` | `/api/admin/users` | Admin | `Default` | User list for moderation (paginated, searchable) |
| `POST` | `/api/admin/users/{userId}/ban` | Admin | `Default` | Ban a user |
| `POST` | `/api/admin/users/{userId}/unban` | Admin | `Default` | Lift a ban |
| `DELETE` | `/api/admin/users/{userId}` | Admin | `Default` | Soft-delete a user; anonymized after the retention window |
| `POST` | `/api/admin/users/{userId}/recover` | Admin | `Default` | Restore a soft-deleted user within the recovery window |
| `POST` | `/api/admin/users/{userId}/roles/{role}` | Admin | `Default` | Grant a role |
| `DELETE` | `/api/admin/users/{userId}/roles/{role}` | Admin | `Default` | Revoke a role (the last admin cannot be demoted) |
| `GET` | `/api/admin/courses` | Admin | `Default` | Course list for moderation, including unpublished and deleted |
| `POST` | `/api/admin/courses/{courseId}/publish` | Admin | `Default` | Publish a course as a moderator |
| `POST` | `/api/admin/courses/{courseId}/unpublish` | Admin | `Default` | Take a course off the catalog |
| `DELETE` | `/api/admin/courses/{courseId}` | Admin | `Default` | Soft-delete a course |
| `POST` | `/api/admin/courses/{courseId}/recover` | Admin | `Default` | Restore a soft-deleted course |
| `GET` | `/api/admin/payments` | Admin | `Default` | All platform payments (paginated, search by email or course) |

## AiChat

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/ai-chat/status` | Authenticated | `Default` | Whether the AI provider is available, and when to retry if it is not |
| `GET` | `/api/ai-chat/platform/session` | Authenticated | `Default` | Session of the platform-wide assistant |
| `GET` | `/api/ai-chat/courses/{courseId}/session` | Authenticated | `Default` | Session of the course tutor (enrollment required) |
| `DELETE` | `/api/ai-chat/platform/session` | Authenticated | `Default` | Clear the assistant session |
| `DELETE` | `/api/ai-chat/courses/{courseId}/session` | Authenticated | `Default` | Clear the tutor session for this course |
| `POST` | `/api/ai-chat/platform/messages` | Authenticated | `AiChatPlatform` | Send a message to the assistant; the reply is streamed (SSE) |
| `POST` | `/api/ai-chat/courses/{courseId}/messages` | Authenticated | `AiChatTutor` | Send a message to the tutor; the body also carries lessonId |

## Auth

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | `AuthStrict` | Register new user |
| `POST` | `/api/auth/confirm-email` | Anonymous | `AuthStrict` | Confirm email via 6-digit OTP (returns JWT + Refresh token) |
| `POST` | `/api/auth/resend-confirmation` | Anonymous | `AuthStrict` | Resend email confirmation |
| `POST` | `/api/auth/forgot-password` | Anonymous | `AuthStrict` | Request password reset |
| `POST` | `/api/auth/reset-password` | Anonymous | `AuthStrict` | Set new password |
| `POST` | `/api/auth/change-password` | Authenticated | `AuthStrict` | Change the password of a user who has one |
| `POST` | `/api/auth/set-password` | Authenticated | `AuthStrict` | Set a first password for an account created via Google |
| `POST` | `/api/auth/login` | Anonymous | `AuthStrict` | Login (returns JWT + Refresh token) |
| `POST` | `/api/auth/refresh` | Anonymous | `Default` | Get new token pair using Refresh cookie |
| `POST` | `/api/auth/google` | Anonymous | `AuthStrict` | Login via Google ID Token |
| `POST` | `/api/auth/logout` | Anonymous | `Default` | Logout and invalidate Refresh token |

## Categories

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/categories` | Anonymous | `Default` | Public category list for the catalog |
| `GET` | `/api/categories/admin` | Admin | `Default` | Category list with course counts, for management |
| `POST` | `/api/categories` | Admin | `Default` | Create a category |
| `PUT` | `/api/categories/{id}` | Admin | `Default` | Rename a category |
| `DELETE` | `/api/categories/{id}` | Admin | `Default` | Delete a category |
| `POST` | `/api/categories/{id}/image` | Admin | `Default` | Attach a cover image (blob path from the SAS upload flow) |
| `DELETE` | `/api/categories/{id}/image` | Admin | `Default` | Remove the cover image; the blob is deleted via the Outbox |

## Certificates

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/certificates/mine` | Authenticated | `Default` | Fetch user's earned certificates |
| `GET` | `/api/certificates/courses/{courseId}` | Authenticated | `Default` | Get certificate details for a specific course |
| `POST` | `/api/certificates/courses/{courseId}/generate` | Authenticated | `Default` | On-demand generate PDF certificate |
| `GET` | `/api/certificates/verify/{code}` | Anonymous | `Default` | Public verification of a certificate by code |

## Config

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/config/public` | Anonymous | `Default` | Public runtime config the SPA needs before login (e.g. the active AI provider) |

## CourseReviews

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/courses/{courseId}/reviews` | Anonymous | `Default` | Reviews of a course (public, paginated) |
| `GET` | `/api/courses/{courseId}/reviews/mine` | Authenticated | `Default` | The current user's own review of the course |
| `POST` | `/api/courses/{courseId}/reviews` | Authenticated + EmailConfirmed | `Default` | Create a review (enrollment is verified) |
| `PUT` | `/api/courses/{courseId}/reviews/{reviewId}` | Authenticated + EmailConfirmed | `Default` | Edit your own review; the course rating is recomputed |
| `DELETE` | `/api/courses/{courseId}/reviews/{reviewId}` | Authenticated | `Default` | Delete a review (author, or admin as moderator) |

## Courses

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/courses` | Anonymous | `Default` | Get public course list (paginated, filtered) |
| `GET` | `/api/courses/featured` | Anonymous | `Default` | Get featured courses |
| `GET` | `/api/courses/{id}` | Anonymous | `Default` | Get course details by ID |
| `GET` | `/api/courses/mine` | Instructor, Admin | `Default` | Get courses created by the current instructor |
| `GET` | `/api/courses/admin` | Admin | `Default` | Get all courses for administration |
| `GET` | `/api/courses/{id}/edit` | Instructor, Admin | `Default` | Get course details for editing |
| `POST` | `/api/courses` | Instructor, Admin | `Default` | Create a new course |
| `PUT` | `/api/courses/{id}` | Instructor, Admin | `Default` | Update course details |
| `POST` | `/api/courses/{id}/publish` | Instructor, Admin | `Default` | Publish a course |
| `POST` | `/api/courses/{id}/unpublish` | Instructor, Admin | `Default` | Unpublish a course |
| `POST` | `/api/courses/{id}/archive` | Instructor, Admin | `Default` | Archive a course |
| `POST` | `/api/courses/{id}/unarchive` | Instructor, Admin | `Default` | Unarchive a course |
| `DELETE` | `/api/courses/{id}` | Instructor, Admin | `Default` | Delete a course |

## Enrollments

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/enrollments` | Authenticated + EmailConfirmed | `Default` | Enroll in a course |
| `GET` | `/api/enrollments/mine` | Authenticated | `Default` | Get current user's enrollments |
| `GET` | `/api/enrollments/continue` | Authenticated | `Default` | The lesson to resume — what the "Continue learning" card links to |

## Instructor

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/instructor/earnings` | Instructor, Admin | `Default` | Instructor earnings grouped by course |

## InstructorApplications

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/instructor-applications` | Authenticated + EmailConfirmed | `Default` | Triggers admin review; spam applications from unverified emails are a moderation risk. |
| `GET` | `/api/instructor-applications/mine` | Authenticated | `Default` | Status of your own instructor application |
| `GET` | `/api/instructor-applications/pending` | Admin | `Default` | Applications awaiting review |
| `POST` | `/api/instructor-applications/{id}/approve` | Admin | `Default` | Approve an application and grant the Instructor role |
| `POST` | `/api/instructor-applications/{id}/reject` | Admin | `Default` | Reject an application with a reason |

## Lessons

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/courses/{courseId}/lessons/{lessonId}` | Authenticated | `Default` | Get lesson content (student view) |
| `POST` | `/api/courses/{courseId}/sections/{sectionId}/lessons/video` | Instructor, Admin | `Default` | Add a video lesson (course owner or admin) |
| `POST` | `/api/courses/{courseId}/sections/{sectionId}/lessons/test` | Instructor, Admin | `Default` | Add a test lesson (course owner or admin) |
| `POST` | `/api/courses/{courseId}/sections/{sectionId}/lessons/post` | Instructor, Admin | `Default` | Add an article lesson (course owner or admin) |
| `PATCH` | `/api/courses/{courseId}/lessons/{lessonId}/video` | Instructor, Admin | `Default` | Update a video lesson (course owner or admin) |
| `PATCH` | `/api/courses/{courseId}/lessons/{lessonId}/test` | Instructor, Admin | `Default` | Update a test lesson (course owner or admin) |
| `PATCH` | `/api/courses/{courseId}/lessons/{lessonId}/post` | Instructor, Admin | `Default` | Update an article lesson (course owner or admin) |
| `PATCH` | `/api/courses/{courseId}/lessons/{lessonId}/toggle-visibility` | Instructor, Admin | `Default` | Show or hide a lesson from students |
| `DELETE` | `/api/courses/{courseId}/lessons/{lessonId}` | Instructor, Admin | `Default` | Delete a lesson; its video blob is removed via the Outbox |
| `POST` | `/api/courses/{courseId}/sections/{sectionId}/lessons/reorder` | Instructor, Admin | `Default` | Reorder lessons within a section |

## Messages

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/messages/conversations` | Authenticated | `Default` | Conversation list (paginated, searchable) |
| `GET` | `/api/messages/conversations/{conversationId}/messages` | Authenticated | `Default` | Message history of a conversation (paginated) |
| `POST` | `/api/messages/conversations/start-or-get` | Authenticated + EmailConfirmed | `ChatMessages` | Open the conversation for a course, creating it on first use |
| `POST` | `/api/messages/conversations/{conversationId}/messages` | Authenticated + EmailConfirmed | `ChatMessages` | Send a message; delivered in real time over SignalR |
| `PUT` | `/api/messages/conversations/{conversationId}/read` | Authenticated | `Default` | Mark the conversation as read |
| `GET` | `/api/messages/unread-count` | Authenticated | `Default` | Total unread message count |

## Notifications

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/notifications` | Authenticated | `Default` | Fetch user's notifications history |
| `GET` | `/api/notifications/unread-count` | Authenticated | `Default` | Get total unread notifications count |
| `POST` | `/api/notifications/{notificationId}/read` | Authenticated | `Default` | Mark one notification as read |
| `POST` | `/api/notifications/read-all` | Authenticated | `Default` | Mark all user's notifications as read |
| `POST` | `/api/notifications/read-by-type` | Authenticated | `Default` | Mark all notifications of a specific type as read |

## Payments

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/payments` | Authenticated + EmailConfirmed | `Payments` | Buy a paid course — creates the Payment and the Enrollment in one transaction |
| `GET` | `/api/payments/mine` | Authenticated | `Default` | Your own payment history (paginated) |

## Progress

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/progress/courses/{courseId}/lessons/{lessonId}/complete` | Authenticated | `Default` | Mark a lesson as completed |
| `GET` | `/api/progress/courses/{courseId}` | Authenticated | `Default` | Get course progress for the current user |

## Sections

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/courses/{courseId}/sections` | Instructor, Admin | `Default` | Create a section |
| `PATCH` | `/api/courses/{courseId}/sections/{sectionId}` | Instructor, Admin | `Default` | Update section title |
| `DELETE` | `/api/courses/{courseId}/sections/{sectionId}` | Instructor, Admin | `Default` | Delete a section |
| `POST` | `/api/courses/{courseId}/sections/reorder` | Instructor, Admin | `Default` | Reorder sections |

## Tests

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/courses/{courseId}/lessons/{lessonId}/test` | Authenticated | `Default` | Get test metadata and current attempt status |
| `POST` | `/api/courses/{courseId}/lessons/{lessonId}/test/attempts/start` | Authenticated | `TestAttempts` | Start a new test attempt |
| `POST` | `/api/courses/{courseId}/lessons/{lessonId}/test/attempts/{attemptId}/submit` | Authenticated | `TestAttempts` | Submit answers and score the attempt |
| `GET` | `/api/courses/{courseId}/lessons/{lessonId}/test/attempts` | Authenticated | `Default` | Get all submitted attempts for a test |
| `GET` | `/api/courses/{courseId}/lessons/{lessonId}/test/attempts/{attemptId}/review` | Authenticated | `Default` |  |

## Uploads

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/uploads/request-url` | Authenticated | `Uploads` | Requests a pre-signed SAS URL for direct-to-Azure file upload into a temporary container. |

## Users

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/users/me` | Authenticated | `Default` | The current user profile |
| `PUT` | `/api/users/me` | Authenticated | `Default` | Update your profile (name, bio, avatar) |
| `GET` | `/api/users/{userId}` | Anonymous | `Default` | Public profile of a user — backs the instructor page |
| `GET` | `/api/users/{userId}/instructor-profile` | Anonymous | `Default` |  |

## Wishlist

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/wishlist` | Authenticated | `Default` | Courses in your wishlist |
| `GET` | `/api/wishlist/count` | Authenticated | `Default` | Wishlist size — drives the header badge |
| `POST` | `/api/wishlist/{courseId}` | Authenticated | `Default` | Add a course to the wishlist |
| `DELETE` | `/api/wishlist/{courseId}` | Authenticated | `Default` | Remove a course from the wishlist |
