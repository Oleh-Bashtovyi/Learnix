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
| `GET` | `/api/v1/achievements/me` | Authenticated | `Default` | Fetch user's achievements and progress counters |
| `POST` | `/api/v1/achievements/{achievementId}/seen` | Authenticated | `Default` | Mark an achievement as seen (clears the 'new' badge) |

## Admin

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/admin/stats` | Admin | `Default` | Platform-wide dashboard counters (users, courses, revenue) |
| `GET` | `/api/v1/admin/users` | Admin | `Default` | User list for moderation (paginated, searchable) |
| `POST` | `/api/v1/admin/users/{userId}/ban` | Admin | `Default` | Ban a user |
| `POST` | `/api/v1/admin/users/{userId}/unban` | Admin | `Default` | Lift a ban |
| `DELETE` | `/api/v1/admin/users/{userId}` | Admin | `Default` | Soft-delete a user; anonymized after the retention window |
| `POST` | `/api/v1/admin/users/{userId}/recover` | Admin | `Default` | Restore a soft-deleted user within the recovery window |
| `POST` | `/api/v1/admin/users/{userId}/roles/{role}` | Admin | `Default` | Grant a role |
| `DELETE` | `/api/v1/admin/users/{userId}/roles/{role}` | Admin | `Default` | Revoke a role (the last admin cannot be demoted) |
| `GET` | `/api/v1/admin/users/{userId}/earnings` | Admin | `Default` | View a specific instructor's earnings |
| `GET` | `/api/v1/admin/courses` | Admin | `Default` | Course list for moderation, including unpublished and deleted |
| `POST` | `/api/v1/admin/courses/{courseId}/publish` | Admin | `Default` | Publish a course as a moderator |
| `POST` | `/api/v1/admin/courses/{courseId}/unpublish` | Admin | `Default` | Take a course off the catalog |
| `DELETE` | `/api/v1/admin/courses/{courseId}` | Admin | `Default` | Soft-delete a course |
| `POST` | `/api/v1/admin/courses/{courseId}/recover` | Admin | `Default` | Restore a soft-deleted course |
| `GET` | `/api/v1/admin/payments` | Admin | `Default` | All platform payments (paginated, search by email or course) |

## AiChat

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/ai-chat/status` | Authenticated | `Default` | Whether the AI provider is available, and when to retry if it is not |
| `GET` | `/api/v1/ai-chat/platform/session` | Authenticated | `Default` | Session of the platform-wide assistant |
| `GET` | `/api/v1/ai-chat/courses/{courseId}/session` | Authenticated | `Default` | Session of the course tutor (enrollment required) |
| `DELETE` | `/api/v1/ai-chat/platform/session` | Authenticated | `Default` | Clear the assistant session |
| `DELETE` | `/api/v1/ai-chat/courses/{courseId}/session` | Authenticated | `Default` | Clear the tutor session for this course |
| `POST` | `/api/v1/ai-chat/platform/messages` | Authenticated | `AiChatPlatform` | Send a message to the assistant; the reply is streamed (SSE) |
| `POST` | `/api/v1/ai-chat/courses/{courseId}/messages` | Authenticated | `AiChatTutor` | Send a message to the tutor; the body also carries lessonId |

## Auth

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/auth/register` | Anonymous | `AuthStrict` | Register new user |
| `POST` | `/api/v1/auth/confirm-email` | Anonymous | `AuthStrict` | Confirm email via 6-digit OTP (returns JWT + Refresh token) |
| `POST` | `/api/v1/auth/resend-confirmation` | Anonymous | `AuthStrict` | Resend email confirmation |
| `POST` | `/api/v1/auth/forgot-password` | Anonymous | `AuthStrict` | Request password reset |
| `POST` | `/api/v1/auth/reset-password` | Anonymous | `AuthStrict` | Set new password |
| `POST` | `/api/v1/auth/change-password` | Authenticated | `AuthStrict` | Change the password of a user who has one |
| `POST` | `/api/v1/auth/set-password` | Authenticated | `AuthStrict` | Set a first password for an account created via Google |
| `POST` | `/api/v1/auth/login` | Anonymous | `AuthStrict` | Login (returns JWT + Refresh token) |
| `POST` | `/api/v1/auth/refresh` | Anonymous | `Default` | Get new token pair using Refresh cookie |
| `POST` | `/api/v1/auth/google` | Anonymous | `AuthStrict` | Login via Google ID Token |
| `POST` | `/api/v1/auth/logout` | Anonymous | `Default` | Logout and invalidate Refresh token |

## Categories

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/categories` | Anonymous | `Default` | Public category list for the catalog |
| `GET` | `/api/v1/categories/admin` | Admin | `Default` | Category list with course counts, for management |
| `POST` | `/api/v1/categories` | Admin | `Default` | Create a category; optionally attaches a cover image in the same call |
| `PUT` | `/api/v1/categories/{id}` | Admin | `Default` | Update a category — rename and optionally set or remove the cover image in a single call |
| `DELETE` | `/api/v1/categories/{id}` | Admin | `Default` | Delete a category |

## Certificates

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/certificates/mine` | Authenticated | `Default` | Fetch user's earned certificates |
| `GET` | `/api/v1/certificates/courses/{courseId}` | Authenticated | `Default` | Get certificate details for a specific course |
| `POST` | `/api/v1/certificates/courses/{courseId}/generate` | Authenticated | `Default` | On-demand generate PDF certificate |
| `GET` | `/api/v1/certificates/verify/{code}` | Anonymous | `Default` | Public verification of a certificate by code |

## Config

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/config/public` | Anonymous | `Default` | Public runtime config the SPA needs before login (e.g. the active AI provider) |

## CourseReviews

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/courses/{courseId}/reviews` | Anonymous | `Default` | Reviews of a course (public, paginated) |
| `GET` | `/api/v1/courses/{courseId}/reviews/mine` | Authenticated | `Default` | The current user's own review of the course |
| `POST` | `/api/v1/courses/{courseId}/reviews` | Authenticated + EmailConfirmed | `Default` | Create a review (enrollment is verified) |
| `PUT` | `/api/v1/courses/{courseId}/reviews/{reviewId}` | Authenticated + EmailConfirmed | `Default` | Edit your own review; the course rating is recomputed |
| `DELETE` | `/api/v1/courses/{courseId}/reviews/{reviewId}` | Authenticated | `Default` | Delete a review (author, or admin as moderator) |

## Courses

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/courses` | Anonymous | `Default` | Get public course list (paginated, filtered) |
| `GET` | `/api/v1/courses/featured` | Anonymous | `Default` | Get featured courses |
| `GET` | `/api/v1/courses/popular-tags` | Instructor, Admin | `Default` | Get the tags most published courses carry, optionally within one category |
| `GET` | `/api/v1/courses/{id}` | Anonymous | `Default` | Get course details by ID |
| `GET` | `/api/v1/courses/mine` | Instructor, Admin | `Default` | Get courses created by the current instructor |
| `GET` | `/api/v1/courses/admin` | Admin | `Default` | Get all courses for administration |
| `GET` | `/api/v1/courses/{id}/edit` | Instructor, Admin | `Default` | Get course details for editing |
| `POST` | `/api/v1/courses` | Instructor, Admin | `Default` | Create a new course |
| `PUT` | `/api/v1/courses/{id}` | Instructor, Admin | `Default` | Update course details |
| `POST` | `/api/v1/courses/{id}/publish` | Instructor, Admin | `Default` | Publish a course |
| `POST` | `/api/v1/courses/{id}/unpublish` | Instructor, Admin | `Default` | Unpublish a course |
| `POST` | `/api/v1/courses/{id}/archive` | Instructor, Admin | `Default` | Archive a course |
| `POST` | `/api/v1/courses/{id}/unarchive` | Instructor, Admin | `Default` | Unarchive a course |
| `DELETE` | `/api/v1/courses/{id}` | Instructor, Admin | `Default` | Delete a course |

## Enrollments

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/enrollments` | Authenticated + EmailConfirmed | `Default` | Enroll in a course |
| `GET` | `/api/v1/enrollments/mine` | Authenticated | `Default` | Get current user's enrollments |
| `GET` | `/api/v1/enrollments/continue` | Authenticated | `Default` | The lesson to resume — what the "Continue learning" card links to |

## Instructor

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/instructor/earnings` | Instructor, Admin | `Default` | Instructor earnings grouped by course |

## InstructorAnalytics

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/instructor/analytics/overview` | Instructor | `Default` |  |
| `GET` | `/api/v1/instructor/analytics/summary` | Instructor | `Default` | Top-level KPIs: Total students, earnings, avg rating, certificates issued |
| `GET` | `/api/v1/instructor/analytics/dynamics` | Instructor | `Default` | Daily aggregated enrollments and earnings between startDate and endDate |
| `GET` | `/api/v1/instructor/analytics/courses/popularity` | Instructor | `Default` | List of courses ordered by enrollment count |
| `GET` | `/api/v1/instructor/analytics/courses/statuses` | Instructor | `Default` | Course count by status (Draft, Published, Archived) |
| `GET` | `/api/v1/instructor/analytics/reviews/distribution` | Instructor | `Default` | Distribution of 1 to 5 star ratings across all courses |
| `GET` | `/api/v1/instructor/analytics/reviews/recent` | Instructor | `Default` | List of recent student reviews across all courses |
| `GET` | `/api/v1/instructor/analytics/reviews/trend` | Instructor | `Default` |  |
| `GET` | `/api/v1/instructor/analytics/tests/performance` | Instructor | `Default` | Average test scores and pass rates per lesson |
| `GET` | `/api/v1/instructor/analytics/engagement` | Instructor | `Default` |  |
| `GET` | `/api/v1/instructor/analytics/engagement/drop-off` | Instructor | `Default` |  |

## InstructorApplications

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/instructor-applications` | Authenticated + EmailConfirmed | `Default` | Triggers admin review; spam applications from unverified emails are a moderation risk. |
| `GET` | `/api/v1/instructor-applications/mine` | Authenticated | `Default` | Status of your own instructor application |
| `GET` | `/api/v1/instructor-applications/pending` | Admin | `Default` | Applications awaiting review |
| `POST` | `/api/v1/instructor-applications/{id}/approve` | Admin | `Default` | Approve an application and grant the Instructor role |
| `POST` | `/api/v1/instructor-applications/{id}/reject` | Admin | `Default` | Reject an application with a reason |

## Lessons

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/courses/{courseId}/lessons/{lessonId}` | Authenticated | `Default` | Get lesson content (student view) |
| `POST` | `/api/v1/courses/{courseId}/sections/{sectionId}/lessons/video` | Instructor, Admin | `Default` | Add a video lesson (course owner or admin) |
| `POST` | `/api/v1/courses/{courseId}/sections/{sectionId}/lessons/test` | Instructor, Admin | `Default` | Add a test lesson (course owner or admin) |
| `POST` | `/api/v1/courses/{courseId}/sections/{sectionId}/lessons/post` | Instructor, Admin | `Default` | Add an article lesson (course owner or admin) |
| `PATCH` | `/api/v1/courses/{courseId}/lessons/{lessonId}/video` | Instructor, Admin | `Default` | Update a video lesson (course owner or admin) |
| `PATCH` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test` | Instructor, Admin | `Default` | Update a test lesson (course owner or admin) |
| `PATCH` | `/api/v1/courses/{courseId}/lessons/{lessonId}/post` | Instructor, Admin | `Default` | Update an article lesson (course owner or admin) |
| `PATCH` | `/api/v1/courses/{courseId}/lessons/{lessonId}/toggle-visibility` | Instructor, Admin | `Default` | Show or hide a lesson from students |
| `DELETE` | `/api/v1/courses/{courseId}/lessons/{lessonId}` | Instructor, Admin | `Default` | Delete a lesson; its video blob is removed via the Outbox |
| `POST` | `/api/v1/courses/{courseId}/sections/{sectionId}/lessons/reorder` | Instructor, Admin | `Default` | Reorder lessons within a section |

## Messages

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/messages/conversations` | Authenticated | `Default` | Conversation list (paginated, searchable) |
| `GET` | `/api/v1/messages/conversations/{conversationId}/messages` | Authenticated | `Default` | Message history of a conversation (paginated) |
| `POST` | `/api/v1/messages/conversations/start-or-get` | Authenticated + EmailConfirmed | `ChatMessages` | Open the conversation for a course, creating it on first use |
| `POST` | `/api/v1/messages/conversations/{conversationId}/messages` | Authenticated + EmailConfirmed | `ChatMessages` | Send a message; delivered in real time over SignalR |
| `PUT` | `/api/v1/messages/conversations/{conversationId}/read` | Authenticated | `Default` | Mark the conversation as read |
| `POST` | `/api/v1/messages/conversations/{conversationId}/block` | Authenticated + EmailConfirmed | `Default` | Block the conversation; only the caller who blocked it can unblock |
| `POST` | `/api/v1/messages/conversations/{conversationId}/unblock` | Authenticated + EmailConfirmed | `Default` | Unblock a conversation; 403 if the caller isn't the one who blocked it |
| `GET` | `/api/v1/messages/unread-count` | Authenticated | `Default` | Total unread message count |

## Notifications

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/notifications` | Authenticated | `Default` | Fetch user's notifications history |
| `GET` | `/api/v1/notifications/unread-count` | Authenticated | `Default` | Get total unread notifications count |
| `POST` | `/api/v1/notifications/{notificationId}/read` | Authenticated | `Default` | Mark one notification as read |
| `POST` | `/api/v1/notifications/read-all` | Authenticated | `Default` | Mark all user's notifications as read |
| `POST` | `/api/v1/notifications/read-by-type` | Authenticated | `Default` | Mark all notifications of a specific type as read |

## Payments

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/payments` | Authenticated + EmailConfirmed | `Payments` | Buy a paid course — creates the Payment and the Enrollment in one transaction |
| `GET` | `/api/v1/payments/mine` | Authenticated | `Default` | Your own payment history (paginated) |

## Progress

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/progress/courses/{courseId}/lessons/{lessonId}/complete` | Authenticated | `Default` | Mark a lesson as completed |
| `GET` | `/api/v1/progress/courses/{courseId}` | Authenticated | `Default` | Get course progress for the current user |

## Sections

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/courses/{courseId}/sections` | Instructor, Admin | `Default` | Create a section |
| `PATCH` | `/api/v1/courses/{courseId}/sections/{sectionId}` | Instructor, Admin | `Default` | Update section title |
| `DELETE` | `/api/v1/courses/{courseId}/sections/{sectionId}` | Instructor, Admin | `Default` | Delete a section |
| `POST` | `/api/v1/courses/{courseId}/sections/reorder` | Instructor, Admin | `Default` | Reorder sections |

## Tests

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test` | Authenticated | `Default` | Get test metadata and current attempt status |
| `POST` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test/attempts/start` | Authenticated | `TestAttempts` | Start a new test attempt |
| `POST` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test/attempts/{attemptId}/submit` | Authenticated | `TestAttempts` | Submit answers and score the attempt |
| `GET` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test/attempts` | Authenticated | `Default` | Get all submitted attempts for a test |
| `GET` | `/api/v1/courses/{courseId}/lessons/{lessonId}/test/attempts/{attemptId}/review` | Authenticated | `Default` |  |

## Uploads

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/uploads/request-url` | Authenticated | `Uploads` | Requests a pre-signed SAS URL for direct-to-Azure file upload into a temporary container. |

## Users

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/users/me` | Authenticated | `Default` | The current user profile |
| `PUT` | `/api/v1/users/me` | Authenticated | `Default` | Update your profile (name, bio, avatar) |
| `GET` | `/api/v1/users/{userId}` | Anonymous | `Default` | Public profile of a user — backs the instructor page |
| `GET` | `/api/v1/users/{userId}/instructor-profile` | Anonymous | `Default` |  |

## Wishlist

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `GET` | `/api/v1/wishlist` | Authenticated | `Default` | Courses in your wishlist |
| `GET` | `/api/v1/wishlist/count` | Authenticated | `Default` | Wishlist size — drives the header badge |
| `POST` | `/api/v1/wishlist/{courseId}` | Authenticated | `Default` | Add a course to the wishlist |
| `DELETE` | `/api/v1/wishlist/{courseId}` | Authenticated | `Default` | Remove a course from the wishlist |
