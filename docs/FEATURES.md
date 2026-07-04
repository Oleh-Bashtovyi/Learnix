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

### Course browsing
- Browse all published courses
- Search by title / keyword
- Filter by: category, rating, duration
- Pagination on course listing
- Sort by: newest, most popular, highest rated

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
