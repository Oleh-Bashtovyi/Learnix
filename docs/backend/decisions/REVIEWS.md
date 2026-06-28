# Learnix — ADR: Course Reviews & Ratings

> Covers Phase 9: B-47 (course reviews), B-48 (denormalized rating).

## Підсумок: що реалізовано

| Endpoint | Що робить |
|---|---|
| `GET /api/courses/{courseId}/reviews` | Отримання списку відгуків курсу (публічно, пагінація) |
| `GET /api/courses/{courseId}/reviews/mine` | Отримання власного відгуку поточного користувача |
| `POST /api/courses/{courseId}/reviews` | Створення відгуку (перевіряється факт зарахування на курс) |
| `PUT /api/courses/{courseId}/reviews/{id}` | Редагування відгуку його автором |
| `DELETE /api/courses/{courseId}/reviews/{id}` | Видалення відгуку (автором або адміністратором) |

---

## ADR-REVIEW-001: PostgreSQL over MongoDB for Course Reviews

**Decision:** Course reviews are stored in the PostgreSQL `CourseReviews` table, not in a MongoDB collection. The entity lives in the EF Core data model with a unique constraint on `(StudentId, CourseId)`, FK to `Courses` (cascade delete), and FK to `AspNetUsers` (restrict delete).

**Why:**
- The data is strictly relational: a review belongs to exactly one student and one course, and the "one review per student per course" invariant requires a unique constraint that is cheapest to enforce in the relational layer.
- Creating a review requires checking enrollment (which lives in PostgreSQL). Doing this check cross-database adds a round trip and makes atomic rollback impossible.
- Listing reviews needs `FirstName`, `LastName`, and `AvatarBlobPath` from `AspNetUsers`. A single `JOIN` in PostgreSQL handles this; MongoDB would require an application-side join (two separate queries).
- Updating the denormalized `AverageRating` on `Course` and inserting the new review can be done in a single `SaveChangesAsync` transaction — no distributed commit required.
- Schema is fixed (`Rating int`, `Comment string?`, timestamps). There are no fields that benefit from MongoDB's schema flexibility.

**Rejected alternatives:**
- MongoDB — appropriate if the schema were variable (e.g., media attachments, structured sub-sections) or if reviews were queried in isolation without joins. Neither applies here.
- PostgreSQL JSONB on `Course` — embeds reviews inside the course document, kills the `(StudentId, CourseId)` uniqueness guarantee and makes partial updates expensive.

---

## ADR-REVIEW-002: Inline Domain Arithmetic for Denormalized Rating

**Decision:** `Course` exposes three domain methods that update `AverageRating` (numeric 4,2) and `ReviewsCount` (int) in memory before `SaveChangesAsync`:

```csharp
AddRating(int rating)         // called by CreateReview handler
UpdateRating(int old, int new) // called by UpdateReview handler
RemoveRating(int rating)      // called by DeleteReview handler
```

Both the review and the updated course statistics are committed in the same `SaveChangesAsync` call. No extra round trip for recalculation.

**Why:**
- A single transaction commit is the simplest, most consistent path. If the rating column and the review row are written atomically, the system can never be in a state where a review exists but the course average is stale.
- Keeping the formulas inside the domain entity enforces the invariant that `ReviewsCount` never drifts from the actual number of reviews in the same transaction. The Application layer just calls a named method; it does not reason about arithmetic.
- For the scale of an LMS (hundreds, not millions, of reviews), the marginal rounding drift from storing a rounded `NUMERIC(4,2)` rather than an integer `RatingSum` is acceptable. A student will not notice ±0.01 on an average.

**Rejected alternatives:**
- Recalculate `AVG(Rating)` from the database after saving the review — correct and drift-free, but requires two transactions or a second query inside the same transaction. The complexity gain does not justify the cost at this scale.
- Store `RatingSum` + `ReviewsCount`, compute `AverageRating` as a C# property — eliminates drift entirely but makes DB-side sorting/filtering by rating impossible without a generated column. `NUMERIC(4,2)` stored column is indexable and sortable directly.
- Trigger or computed column in PostgreSQL — removes application logic but makes the schema harder to run locally and adds DDL complexity.

---

## ADR-REVIEW-003: Navigation Property on `CourseReview` for Student Info

**Decision:** `CourseReview` declares `public User? Student { get; private set; }` — an EF navigation property loaded via `Query.Include(r => r.Student)` in `CourseReviewsByCoursePaginatedSpecification`. The listing query returns `FirstName`, `LastName`, and `AvatarBlobPath` from the joined `User` row.

**Why:**
- A single `JOIN` in EF (via `Include`) loads all student info in one round trip. The alternative (separate `IUserRepository` query per review) creates an N+1 problem or requires an explicit `WHERE Id IN (...)` batch query — both more code, no benefit.
- The navigation property is read-only (`private set`) and only populated when explicitly included. No other spec includes it, so there is no accidental eager-loading elsewhere.
- This pattern is already established in the codebase: `Enrollment` has `public Course? Course { get; private set; }` under the same access pattern.

**Rejected alternatives:**
- `IUserRepository.GetByIdsAsync(studentIds)` in the query handler — explicit batch query, correct behavior, but shifts join logic from the ORM into the Application layer and requires an extra infrastructure interface method.
- Store `StudentFirstName`, `StudentLastName` in `CourseReview` (denormalized snapshot) — avoids the join entirely and preserves the display name even if the user later changes it. More storage, more complexity; rejected because this project doesn't need historical display names on reviews.

---

## ADR-REVIEW-004: Review Visibility Rules

**Decision:**

| Actor | Can write | Can read own | Can read all |
|---|---|---|---|
| Enrolled student (payment confirmed) | Yes | Yes | No |
| Student not enrolled | No | No | No |
| Instructor of that course | No (own course) | — | Yes |
| Instructor of another course | Yes (if enrolled) | Yes | No |
| Admin | No (N/A) | N/A | Yes |

- `GET /api/courses/{courseId}/reviews` — instructor (own course) + admin only, paginated, includes student `FirstName`, `LastName`, `AvatarBlobPath`.
- `GET /api/courses/{courseId}/reviews/mine` — any authenticated user; returns `null` if no review exists.
- `POST /api/courses/{courseId}/reviews` — enrollment check (any enrollment row for `StudentId + CourseId`) + not the instructor of that course + no duplicate.
- `PUT /api/courses/{courseId}/reviews/{id}` — review author only.
- `DELETE /api/courses/{courseId}/reviews/{id}` — review author or admin.

**Why:**
- Instructors reviewing their own courses would create a conflict of interest. The check uses `course.InstructorId == currentUser.UserId` — no role check needed, just entity ownership.
- Enrollment check uses `EnrollmentByStudentAndCourseSpecification` (any status) rather than `ActiveEnrollmentByStudentAndCourseSpecification` (Active + PaymentCompleted). A student who completed the course (`Status = Completed`) should still be able to review. Both active and completed students have legitimate reviews.
- Admin delete capability is needed for moderation without requiring admin to be enrolled.

**Rejected alternatives:**
- Allow any authenticated user to review (no enrollment gate) — reduces fake/spam review risk is the enrollment check's main purpose.
- Allow students to see all reviews publicly — reasonable UX, but not in the current feature spec. Can be added as a separate public endpoint later.

