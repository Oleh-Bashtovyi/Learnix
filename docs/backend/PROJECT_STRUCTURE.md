# Learnix Backend Project Structure

## Project Structure Reference

```text
[Repository Root]
+-- .editorconfig       < Global code formatting and style rules (C#, TS, JS, MD)
+-- .husky/             < Git hooks (pre-commit format/build validation)

Learnix.Domain/
+-- Common/             < BaseEntity, SoftDeletableEntity, interfaces (IAuditable, IHasDomainEvents,
│                         ISoftDeletable, IOrderable, IDomainEvent), DomainEvent base record,
│                         ReorderValidation helper, DomainException
+-- Constants/          < Roles, UserConstants, CourseConstants, LessonConstants, etc.
+-- Entities/           < User, Course, Section, Lesson (Video/Post/Test), Category,
│                         Enrollment, Certificate, CourseReview, LessonProgress,
│                         TestAttempt, RefreshToken
+-- Events/
│   +-- (root)          < UserRegisteredDomainEvent, PasswordResetRequestedDomainEvent
│   +-- User/           < UserAvatarSetDomainEvent, UserAvatarRemovedDomainEvent
│   +-- Course/         < CourseCreatedDomainEvent, CoursePublishedDomainEvent, etc.
│   L-- Lessons/        < LessonVideoAttachedDomainEvent, LessonVideoReleasedDomainEvent
L-- Enums/              < CourseStatus, LessonType, QuestionType, EnrollmentStatus, etc.

Learnix.Application/
+-- Common/
│   +-- Abstractions/
│   │   +-- Identity/       < ICurrentUserService
│   │   +-- Messaging/      < IEmailSender
│   │   +-- Persistence/    < IUnitOfWork
│   │   L-- Storage/        < IBlobStorageService, UploadTarget, UploadUrlResponse, BlobMetadata
│   +-- Behaviors/      < LoggingBehavior, ValidationBehavior, DomainExceptionBehavior
│   +-- Commands/       < CourseCommandHandler<,>, CourseSectionCommandHandler<,>
│   +-- Constants/      < CommonMessages
│   +-- Errors/         < NotFoundError, ConflictError, ForbiddenError, AuthenticationError, ValidationError
│   +-- Events/         < DomainEventNotification<T>
│   +-- Extensions/     < ICurrentUserService extensions (IsOwnerOrAdmin, etc.)
│   +-- Models/         < ReorderItem
│   +-- Pagination/     < PaginatedResult<T>, PaginationRequest
│   L-- Settings/       < AppSettings, JwtSettings, BlobStorageOptions
+-- Auth/
│   +-- Abstractions/   < IUserRegistrationService, IUserAuthenticationService, ITokenService,
│   │                     IRefreshTokenRepository, IGoogleTokenValidator, IPasswordResetService
│   +-- Commands/       < Register, ConfirmEmail, Login, Logout, RefreshToken, GoogleLogin,
│   │                     ForgotPassword, ResetPassword, ResendConfirmationEmail
│   +-- EventHandlers/  < UserRegisteredDomainEventHandler, PasswordResetRequestedDomainEventHandler
│   +-- Constants/      < AuthValidationConstants
│   +-- Models/         < UserAuthenticationInfo, AccessTokenResult, RefreshTokenResult
│   +-- Specifications/ < RefreshTokenByHashSpecification, ActiveRefreshTokensByUserSpecification
│   L-- Validation/     < PasswordRules (FluentValidation extension)
+-- Courses/
│   +-- Abstractions/   < ICourseRepository, ICategoryRepository, IPublicCourseCatalogSearchService
│   +-- Commands/       < CreateCourse, UpdateCourseDetails, PublishCourse, UnpublishCourse,
│   │                     ArchiveCourse, DeleteCourse
│   +-- Queries/        < GetCourseById, GetCourseForEditById, GetPublicCourses (+ instructorId filter),
│   │                     GetInstructorCourses, GetAdminCourses
│   L-- Specifications/ < CourseByIdSpecification, CourseListSpecification, CourseListCountSpecification
+-- Sections/
│   L-- Commands/       < CreateSection, UpdateSectionTitle, DeleteSection, ReorderSections
+-- Lessons/
│   +-- Abstractions/   < ILessonRepository
│   +-- Commands/       < CreateVideoLesson, CreatePostLesson, CreateTestLesson,
│                         UpdateVideoLesson, UpdatePostLesson, UpdateTestLesson,
│                         DeleteLesson, ReorderLessons, ToggleLessonVisibility
│   L-- Queries/        < GetTestLesson
+-- TestAttempts/
│   +-- Commands/       < StartTestAttempt, SubmitTestAttempt
│   L-- Queries/        < GetTestAttempt
+-- Uploads/
│   L-- Commands/       < RequestUploadUrl
+-- Enrollments/
│   L-- Commands/       < (enrollment flows)
+-- Users/
│   +-- Abstractions/   < IUserRepository
│   +-- Commands/       < UpdateProfile
│   +-- Queries/        < GetMyProfile, GetUserProfile
│   L-- Specifications/ < UserByIdSpecification
+-- Achievements/       < User achievements & progress
+-- Admin/              < Admin-specific actions
+-- AiChat/             < Chatbot & completions
+-- Certificates/       < Certificate generation
+-- Config/             < Settings models
+-- LessonProgress/     < Lesson completion tracking
+-- Messaging/          < Direct messaging & conversations
+-- Notifications/      < In-app user notifications
+-- Payments/           < Payment flows & webhooks
+-- Reviews/            < Course reviews
+-- Wishlist/           < User wishlist operations
L-- Categories/
    +-- Commands/       < CreateCategory, UpdateCategory, DeleteCategory
    L-- Queries/        < GetCategories

Learnix.Infrastructure/
+-- Persistence/
│   +-- EntityFramework/
│   │   +-- Configurations/   < EF entity type configurations
│   │   +-- Interceptors/     < AuditableInterceptor, SoftDeleteInterceptor, DomainEventsInterceptor
│   │   +-- Repositories/     < CourseRepository, LessonRepository, CategoryRepository,
│   │                           RefreshTokenRepository, UserRepository
│   │   L-- ApplicationDbContext.cs
│   L-- Mongo/                < Mongo db components
+-- Outbox/
│   +-- OutboxMessage.cs
│   +-- IOutboxMessageDispatcher.cs
│   +-- OutboxMessageDispatcher.cs
│   L-- OutboxMessageTypes.cs
+-- Email/                    < EmailRenderer, SmtpEmailSender, SmtpSettings
+-- Identity/                 < UserRegistrationService, UserAuthenticationService, JwtTokenService,
│                               PasswordResetService, GoogleTokenValidator, CurrentUserService
+-- Services/
│   +-- Catalog/              < PublicCourseCatalogSearchService, FeaturedCoursesService
│   +-- Certificates/         < CertificatePdfDocument
│   +-- HostedServices/
│   │   +-- Cleanup/          < ChatSessionCleanupService, RefreshTokenCleanupHostedService
│   │   L-- Maintenance/      < CategoryCoursesCountReconciliationService
│   L-- Outbox/               < OutboxProcessorService, OutboxNotificationListener
+-- AiChat/                   < Semantic Kernel integrations
+-- Assets/                   < Static assets (fonts, etc.)
+-- Hubs/                     < SignalR hubs (Chat, Notifications)
+-- Storage/                  < AzureBlobStorageService
L-- DependencyInjection.cs
Learnix.DbMigrator/
+-- Seeders/
   +-- System/         < AdminSeeder, RoleSeeder, CategorySeeder
   L-- Demo/           < CourseSeeder, StudentSeeder, CourseSeeders (definitions)
L-- Program.cs          < EF Migration Runner + Seeder execution logic

Learnix.API/
+-- Controllers/        < AuthController, CoursesController, SectionsController, LessonsController,
│                         TestsController, UploadsController, UsersController, CategoriesController
+-- Extensions/         < ResultExtensions (ToActionResult), WebApplicationExtensions
+-- Middleware/         < ExceptionHandlingMiddleware, SecurityHeadersMiddleware
L-- RateLimiting/       < RateLimitPolicies
```

---
