# Learnix — Feature Specification

---

## 1. Authentication & Authorization

### Registration / Login
- Email + password registration
- Login via email + password
- OAuth 2.0 — Google (register and login)
- JWT access token + refresh token rotation
- HttpOnly cookie for refresh token

### Email verification
- Confirmation email sent after registration
- Account is restricted until email is confirmed
- Resend confirmation email option

### Password reset
- "Forgot password" flow — sends reset link to email
- Token-based reset link with expiry
- Invalidate token after use

### Roles
| Role | Description |
|---|---|
| `Student` | Default role on registration |
| `Instructor` | Granted after submitting an application approved by Admin |
| `Admin` | Full platform access |

### Instructor application flow
- Any Student can submit an "Become an Instructor" application
- Application statuses: `Pending` / `Approved` / `Rejected`
- Admin sees list of pending applications in the admin panel
- On approval → user's role is upgraded to `Instructor`, notification sent
- On rejection → notification sent with optional reason

---

## 1.5. Course Lifecycle

| Status | Visible to | Mutable | Can enroll |
|---|---|---|---|
| `Draft` | Owner, Admin | Yes | No |
| `Published` | Everyone | Yes (via Unpublish → Draft → edit → Publish) | Yes |
| `Archived` | Owner, Admin | Read-only | No |

**Publish invariants** (all must hold):
1. `CoverImageUrl` is set
2. Course has at least one section
3. At least one section has at least one lesson

---

## 2. Student Features

### Course browsing (catalog)

A visitor — signed in or not — can find a course among all published ones by searching,
filtering, sorting and paging through the catalog, and can control how many cards a page holds.

**Behaviour**

1. Only courses with `Status = Published` are ever returned. Drafts and archived courses are
   invisible to everyone here, including their own instructor.
2. Anonymous access is allowed (`[AllowAnonymous]`) — no login needed to browse.
3. Search matches the course **title only**, case-insensitively, as a substring
   (`ILike '%term%'`). It does not match description, tags, category or instructor name.
   Max length 100 chars.
4. Filters, all optional and combinable (AND):
   - `categoryId` — exact category
   - `isFree` — `true` → price = 0; `false` → price > 0
   - `minRating` — average rating ≥ value; must be within 0–5
   - `instructorId` — exact instructor
5. Sort — one of `popular` (default), `newest`, `rating`. Any other value is a 400.
   - `popular` → enrollments desc, then last-updated desc
   - `newest` → created desc
   - `rating` → average rating desc, then reviews count desc
6. **Relevance override:** when a search term is given and no explicit sort is chosen, results are
   ordered by relevance instead of popularity — exact title match first, then titles starting with
   the term, then the rest; ties broken by enrollments, then last-updated. An explicit `sortBy`
   always wins over relevance.
7. Pagination is offset-based (`skip` / `take`). `take` must be 1–100; default 20.
   The response carries the total count so the client can render page numbers.
8. Page size is user-controlled on the client: 12 / 24 / 48 on desktop, 12 / 24 on mobile
   (a small screen never has to render 48 cards). Default 12. If the viewport shrinks below the
   current size's allowance, the size clamps down to the largest option still permitted.
9. Every filter, the sort, the page and the page size live in the **URL query string**, so a
   catalog view is shareable and survives a reload. Default values are omitted from the URL
   (page 1, `popular`, size 12 produce a clean `/courses`).
10. Changing any filter, the sort or the page size resets paging to page 1.
11. The search box is debounced 350 ms before it touches the URL or fires a request.
12. A card shows: cover, title, description, price (or "Free"), category name, instructor full
    name, average rating, reviews count, enrollments count and tags.

**API**

`GET /api/courses?search=&skip=0&take=20&categoryId=&instructorId=&sortBy=&isFree=&minRating=`
→ `PaginatedResult<PublicCourseCardDto>`

**Edge cases**

- No matches → empty page with `totalCount = 0`, not a 404. The UI shows an empty state with a
  "clear filters" action.
- `take > 100`, `minRating > 5`, `skip < 0`, unknown `sortBy` → 400 with a ProblemDetails body.
- A course unpublished while a visitor is on page 3 simply disappears from subsequent pages;
  offset paging is allowed to shift under the reader.

**Known limitations**

- Results are cached in Redis for 5 minutes, keyed by the normalized query. There is **no
  explicit invalidation** — `IDistributedCache` has no prefix/tag deletion — so a newly published
  course can take up to 5 minutes to appear in the catalog. Accepted trade-off.
- Search is a substring `ILIKE`, not full-text: no stemming, no ranking, no typo tolerance, and no
  index can serve a leading wildcard. Fine at seed scale; would not survive a real catalog.
- There is **no duration filter**.

### Course enrollment
- Enroll in free courses instantly
- Enroll in paid courses after mock payment
- View enrolled courses in personal dashboard
- Add courses to Wishlist

### Lesson viewing
- View video lessons (with player)
- View post lessons (with markdown rendering)
- Mark lesson as completed

### Tests
- Take tests attached to lessons
- See score and correct answers after submission
- Retry based on attempt settings

### Profile
- Edit display name, avatar, bio
- View earned achievements and certificates

### Achievements
- Awarded automatically based on actions
- Tracked via robust backend evaluator

### AI Assistant
- Chat widget available on all pages
- Sends question to server → processed by AI (Anthropic/Gemini)
- Conversation history saved per user session (MongoDB)
- Streaming response (SSE)

---

## 3. Instructor Features

### Course management
- Create / edit / delete courses
- Set course details, price, cover image, tags
- Course status: `Draft` / `Published` / `Archived`

### Course structure
- Courses are divided into **Sections** (modules)
- Sections contain **Lessons**
- Lessons can be reordered via drag-and-drop

### Lesson types
- **Video lesson**: Video file attachment (uploaded to Azure Blob)
- **Post lesson**: Rich text description with markdown support
- **Assignment (Test)**: One or more questions, attempt settings

### Instructor dashboard
- List of own courses with enrollment stats
- Detailed metrics

---

## 4. Admin Features

- View and manage all users (search, ban/unban, assign roles)
- View all courses (published and drafts), unpublish or delete
- Review Instructor applications
- View mock payment history

---

## 5. Tests (Quiz System)

### Question types
- Single choice, Multiple choice, Text input (ignore case, fuzzy match)

### Attempt settings
- Unlimited attempts or Limited attempts with cooldown

### Scoring
- Percentage scoring, configurable passing threshold

---

## 6. Achievements System

Awarded automatically when conditions are met. Examples:
- **First Step**: Complete first lesson
- **Getting Started**: Complete 5 lessons
- **Course Graduate**: Complete 1 course

---

## 7. Chat (Student ↔ Instructor)

- 1-on-1 messaging per course context
- Real-time updates via SignalR
- Message history stored in PostgreSQL (CourseConversation, CourseMessage)

---

## 8. Certificates

- Generated automatically (PDF) on course completion
- Available for download and shareable via URL

---

## 9. Mock Payment

- Checkout flow with mock validation
- Creates `Enrollment` on success and records `Payment`

---

## 10. Notifications

- In-app notification bell (top navbar)
- Real-time push via SignalR
- Stored in PostgreSQL, tracks unread counts dynamically

---

## 11. Course Ratings & Reviews

- Student can leave a rating (1–5 stars) and text review
- Average rating shown on course card
- Data stored in PostgreSQL with denormalized counts on Course entity

---

## 12. Security

- Rate limiting on auth endpoints
- JWT short expiry + refresh token rotation
- File upload validation

---

## 13. Localization & Responsiveness

- **Multi-language (i18n)**: Fully supported with persistent language preferences.
- **Mobile responsiveness**: Fully adapted for mobile web browsers.

---

## Out of Scope for v1

- Native Mobile app (iOS/Android)
- Live video / webinars
- Course comments / forum threads
- Affiliate / referral system
- Real Instructor payouts (via Stripe Connect)
