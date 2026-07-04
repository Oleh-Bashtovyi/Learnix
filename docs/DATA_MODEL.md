# Learnix — Data Model

> **Note:** This is a living document. Updated to reflect the complete codebase.
> PostgreSQL entities are mapped via EF Core. MongoDB documents via MongoDB.Driver.

---

## PostgreSQL Entities

---

### User
Primary identity entity. Managed by ASP.NET Core Identity (`IdentityUser<Guid>` base).
Implements `IAuditable`, `IHasDomainEvents` directly.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK (from `IdentityUser<Guid>`) |
| `Email` | `string` | Unique, required |
| `NormalizedEmail` | `string` | Identity-managed |
| `UserName` | `string` | Mirrors Email |
| `PasswordHash` | `string?` | Null for Google-only accounts |
| `EmailConfirmed` | `bool` | Default: false |
| `FirstName` | `string` | Required, max 100 |
| `LastName` | `string` | Required, max 100 |
| `AvatarBlobPath` | `string?` | Azure Blob path (not a full URL) |
| `Bio` | `string?` | Max 500 chars |
| `GoogleId` | `string?` | For Google OAuth accounts |
| `CreatedAt` | `DateTime` | UTC, set by `AuditableInterceptor` |
| `UpdatedAt` | `DateTime` | UTC, set by `AuditableInterceptor` |

**Relations:**
- Has many `Enrollment`
- Has many `LessonProgress`
- Has many `TestAttempt`
- Has many `Certificate`
- Has many `CourseReview`
- Has many `Course` (as instructor)
- Has many `RefreshToken`
- Has many `Payment`
- Has many `Notification`
- Has many `UserAchievement`
- Has many `WishlistItem`
- Has one `InstructorApplication`
- Has one `UserAchievementProgress`

---

### RefreshToken
Stored hashed refresh tokens for JWT rotation.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `TokenHash` | `string` | SHA-256 hash |
| `ExpiresAt` | `DateTime` | UTC |
| `IsRevoked` | `bool` | Default: false |
| `RevokedAt` | `DateTime?` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### OutboxMessage
Reliable blob-storage and event operation queue. Processed by a background worker.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Type` | `string` | Message type (e.g., DeleteBlob, DomainEvent) |
| `Payload` | `string` | JSON payload |
| `OccurredAt` | `DateTime` | UTC |
| `ProcessedAt` | `DateTime?` | UTC |
| `AttemptCount` | `int` | |
| `LastAttemptAt` | `DateTime?` | UTC |
| `LastError` | `string?` | |
| `NextRetryAt` | `DateTime?` | UTC |

---

### InstructorApplication
Student submits to become an Instructor. Admin reviews.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User, Unique |
| `MotivationText` | `string` | |
| `PortfolioUrl` | `string?` | |
| `Status` | `ApplicationStatus` | Pending / Approved / Rejected |
| `RejectionReason` | `string?` | |
| `ReviewedByAdminId` | `Guid?` | FK → User |
| `CreatedAt` | `DateTime` | UTC |
| `ReviewedAt` | `DateTime?` | UTC |

---

### Category

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Name` | `string` | Unique |
| `Slug` | `string` | URL-friendly |
| `IsSystem` | `bool` | Protected from deletion |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### UserCompletedCategory
Tracks which course categories a user has completed.

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | FK → User, PK composite |
| `CategoryId` | `Guid` | FK → Category, PK composite |

---

### Course

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `InstructorId` | `Guid` | FK → User |
| `CategoryId` | `Guid` | FK → Category |
| `Title` | `string` | |
| `Description` | `string` | Markdown |
| `CoverBlobPath` | `string?` | |
| `Price` | `decimal` | 0 = free |
| `Status` | `CourseStatus` | Draft / Published / Archived |
| `EnrollmentsCount` | `int` | Denormalized |
| `AverageRating` | `decimal` | Denormalized |
| `ReviewsCount` | `int` | Denormalized |
| `Tags` | `string[]` | PostgreSQL array |
| `IsDeleted` | `bool` | Soft delete |
| `DeletedAt` | `DateTime?` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### Section

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `Title` | `string` | |
| `DisplayOrder` | `int` | |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### Lesson (TPH)

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `SectionId` | `Guid` | FK → Section |
| `Title` | `string` | |
| `DisplayOrder` | `int` | |
| `IsHidden` | `bool` | |
| `LessonType` | `LessonType` | Video / Post / Test |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

#### VideoLesson
| Field | Type | Notes |
|---|---|---|
| `VideoBlobPath` | `string` | |
| `Description` | `string?` | |
| `DurationSeconds` | `int?` | |

#### PostLesson
| Field | Type | Notes |
|---|---|---|
| `Content` | `string` | Markdown |

#### TestLesson
| Field | Type | Notes |
|---|---|---|
| `Description` | `string?` | |
| `AttemptLimit` | `int?` | |
| `CooldownMinutes` | `int?` | |
| `PassingThreshold` | `int` | |
| `Questions` | JSONB | Owned collection |

---

### Question, QuestionOption, TextAnswerConfig
**Owned types** stored as JSONB inside `TestLesson`.

---

### TestAttempt

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `TestLessonId` | `Guid` | FK → Lesson |
| `AttemptNumber` | `int` | |
| `StartedAt` | `DateTime` | UTC |
| `SubmittedAt` | `DateTime?` | UTC |
| `Score` | `int?` | Percentage |
| `MaxScore` | `int?` | |
| `Passed` | `bool?` | |
| `Answers` | JSONB | Owned value objects |

---

### Enrollment

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `Status` | `EnrollmentStatus` | Active / Completed |
| `PaymentStatus` | `PaymentStatus` | Pending / Completed / Failed |
| `PricePaid` | `decimal` | |
| `EnrolledAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |
| `CreatedAt` | `DateTime` | UTC |
| `UpdatedAt` | `DateTime` | UTC |

---

### LessonProgress

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `LessonId` | `Guid` | FK → Lesson |
| `IsCompleted` | `bool` | |
| `LastAccessedAt` | `DateTime` | UTC |
| `CompletedAt` | `DateTime?` | UTC |

---

### Certificate

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `StudentId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `EnrollmentId` | `Guid` | FK → Enrollment |
| `Code` | `string` | Unique |
| `FileUrl` | `string?` | Azure Blob URL |
| `IssuedAt` | `DateTime` | UTC |

---

### CourseReview

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `StudentId` | `Guid` | FK → User |
| `Rating` | `int` | 1–5 |
| `Comment` | `string?` | |

---

### CourseConversation & CourseMessage
Chat between Student and Instructor.

#### CourseConversation
| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `CourseId` | `Guid` | FK → Course |
| `StudentId` | `Guid` | FK → User |
| `InstructorId` | `Guid` | FK → User |
| `StudentUnreadCount` | `int` | |
| `InstructorUnreadCount`| `int` | |

#### CourseMessage
| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `ConversationId` | `Guid` | FK → CourseConversation |
| `SenderId` | `Guid` | FK → User |
| `Content` | `string` | |

---

### WishlistItem
| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |

---

### UserAchievement & UserAchievementProgress

#### UserAchievement
Records earned achievements.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `AchievementId` | `Guid` | FK → Achievement |
| `EarnedAt` | `DateTime` | UTC |

#### UserAchievementProgress
Denormalized cache of per-user counters for the achievements page.

| Field | Type | Notes |
|---|---|---|
| `UserId` | `Guid` | FK → User, PK |
| `LessonsCompleted` | `int` | |
| `CoursesCompleted` | `int` | |
| `DistinctCategoriesCompleted`| `int` | |
| `ProfileCompleted` | `bool` | |

---

### Payment

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `CourseId` | `Guid` | FK → Course |
| `Amount` | `decimal` | |
| `Currency` | `string` | |
| `Status` | `PaymentStatus` | Pending / Completed / Failed |

---

### Notification

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `UserId` | `Guid` | FK → User |
| `Type` | `NotificationType`| |
| `Title` | `string` | |
| `Body` | `string` | |
| `IsRead` | `bool` | |
| `RelatedEntityId`| `Guid?` | |

---

## MongoDB Documents

### ChatSession
AI assistant conversation history per user.

```json
{
  "_id": "ObjectId",
  "userId": "Guid (string)",
  "isActive": "bool",
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "closedAt": "DateTime?",
  "messages": [
    {
      "role": "user | assistant | tool_result",
      "content": "string",
      "toolCalls": [
        {
          "callId": "string",
          "toolName": "string",
          "argumentsJson": "string",
          "resultJson": "string?"
        }
      ],
      "sentAt": "DateTime"
    }
  ]
}
```

---

## Key Relations Summary

```
User ──< Enrollment >── Course
User ──< LessonProgress >── Lesson
User ──< TestAttempt >── TestLesson
User ──< Certificate >── Enrollment
User ──< CourseReview >── Course
User ──< WishlistItem >── Course
User ──< UserAchievement >── Achievement
User ──< CourseConversation >── Course
CourseConversation ──< CourseMessage
Course ──< Section ──< Lesson
TestLesson ──< Question (JSONB) ──< QuestionOption (nested)
TestAttempt ──< StudentAnswer (JSONB)
```
