# Learnix — Backend Project Structure

## Project Structure Reference

```
Learnix.Domain/
+-- Common/             < BaseEntity, SoftDeletableEntity, interfaces (IAuditable, IHasDomainEvents,
¦                         ISoftDeletable, IOrderable, IDomainEvent), DomainEvent base record,
¦                         ReorderValidation helper, DomainException
+-- Constants/          < Roles, UserConstants, CourseConstants, LessonConstants, etc.
+-- Entities/           < User, Course, Section, Lesson (Video/Post/Test), Category,
¦                         Enrollment, Certificate, CourseReview, LessonProgress,
¦                         TestAttempt, RefreshToken, OutboxMessage
+-- Events/
¦   +-- (root)          < UserRegisteredDomainEvent, PasswordResetRequestedDomainEvent
¦   +-- User/           < UserAvatarSetDomainEvent, UserAvatarRemovedDomainEvent
¦   +-- Course/         < CourseCreatedDomainEvent, CoursePublishedDomainEvent, etc.
¦   L-- Lessons/        < LessonVideoAttachedDomainEvent, LessonVideoReleasedDomainEvent
L-- Enums/              < CourseStatus, LessonType, QuestionType, EnrollmentStatus, etc.

Learnix.Application/
+-- Common/
¦   +-- Abstractions/
¦   ¦   +-- Identity/       < ICurrentUserService
¦   ¦   +-- Messaging/      < IEmailSender
¦   ¦   +-- Persistence/    < IUnitOfWork
¦   ¦   L-- Storage/        < IBlobStorageService, UploadTarget, UploadUrlResponse, BlobMetadata
¦   +-- Behaviors/      < LoggingBehavior, ValidationBehavior, DomainExceptionBehavior
¦   +-- Commands/       < CourseCommandHandler<,>, CourseSectionCommandHandler<,>
¦   +-- Constants/      < CommonMessages
¦   +-- Errors/         < NotFoundError, ConflictError, ForbiddenError, AuthenticationError, ValidationError
¦   +-- Events/         < DomainEventNotification<T>
¦   +-- Extensions/     < ICurrentUserService extensions (IsOwnerOrAdmin, etc.)
¦   +-- Models/         < ReorderItem
¦   +-- Pagination/     < PaginatedResult<T>, PaginationRequest
¦   L-- Settings/       < AppSettings, JwtSettings, BlobStorageOptions
+-- Auth/
¦   +-- Abstractions/   < IUserRegistrationService, IUserAuthenticationService, ITokenService,
¦   ¦                     IRefreshTokenRepository, IGoogleTokenValidator, IPasswordResetService
¦   +-- Commands/       < Register, ConfirmEmail, Login, Logout, RefreshToken, GoogleLogin,
¦   ¦                     ForgotPassword, ResetPassword, ResendConfirmationEmail
¦   +-- EventHandlers/  < UserRegisteredDomainEventHandler, PasswordResetRequestedDomainEventHandler
¦   +-- Constants/      < AuthValidationConstants
¦   +-- Models/         < UserAuthenticationInfo, AccessTokenResult, RefreshTokenResult
¦   +-- Specifications/ < RefreshTokenByHashSpecification, ActiveRefreshTokensByUserSpecification
¦   L-- Validation/     < PasswordRules (FluentValidation extension)
+-- Courses/
¦   +-- Abstractions/   < ICourseRepository, ICategoryRepository, IPublicCourseCatalogSearchService
¦   +-- Commands/       < CreateCourse, UpdateCourseDetails, PublishCourse, UnpublishCourse,
¦   ¦                     ArchiveCourse, DeleteCourse
¦   +-- Queries/        < GetCourseById, GetCourseForEditById, GetPublicCourses (+ instructorId filter),
¦   ¦                     GetInstructorCourses, GetAdminCourses
¦   L-- Specifications/ < CourseByIdSpecification, CourseListSpecification, CourseListCountSpecification
+-- Sections/
¦   L-- Commands/       < CreateSection, UpdateSectionTitle, DeleteSection, ReorderSections
+-- Lessons/
¦   +-- Abstractions/   < ILessonRepository
¦   L-- Commands/       < CreateVideoLesson, CreatePostLesson, UpdateVideoLesson, UpdatePostLesson,
¦                         DeleteLesson, ReorderLessons
+-- Tests/
¦   +-- Commands/       < CreateTestLesson, UpdateTestLesson, UpdateTestSettings
¦   L-- Queries/        < GetTestLesson, GetTestAttempt
+-- Uploads/
¦   L-- Commands/       < RequestUploadUrl
+-- Enrolment/
¦   L-- Commands/       < (enrollment flows)
+-- Users/
¦   +-- Abstractions/   < IUserRepository
¦   +-- Commands/       < UpdateProfile
¦   +-- Queries/        < GetMyProfile, GetUserProfile
¦   L-- Specifications/ < UserByIdSpecification
L-- Categories/
    +-- Commands/       < CreateCategory, UpdateCategory, DeleteCategory
    L-- Queries/        < GetCategories

Learnix.Infrastructure/
 Persistence/
    EntityFramework/
       ApplicationDbContext.cs
       Configurations/       EF entity type configurations
       Interceptors/         AuditableInterceptor, SoftDeleteInterceptor, DomainEventsInterceptor
       Repositories/         CourseRepository, LessonRepository, CategoryRepository,
                               RefreshTokenRepository, UserRepository
       Migrations/
    Mongo/                  Mongo db components
 Outbox/
    OutboxMessage.cs      (also in Domain/Entities - cross-reference)
    IOutboxMessageDispatcher.cs
    OutboxMessageDispatcher.cs
    OutboxMessageTypes.cs
 Email/                  EmailRenderer, SmtpEmailSender, SmtpSettings
 Identity/             UserRegistrationService, UserAuthenticationService, JwtTokenService,
                         PasswordResetService, GoogleTokenValidator, CurrentUserService
 Services/
    Catalog/            PublicCourseCatalogSearchService, FeaturedCoursesService
    Certificates/       CertificatePdfDocument
        HostedServices/
       Cleanup/        ChatSessionCleanupService, RefreshTokenCleanupHostedService
       Maintenance/    CategoryCoursesCountReconciliationService
       Seeders/        AdminSeederHostedService, RoleSeederHostedService, CourseSeederHostedService, StudentSeederHostedService, CategorySeederHostedService
    Outbox/             OutboxProcessorService, OutboxNotificationListener
 Storage/              AzureBlobStorageService
 DependencyInjection.cs

Learnix.API/
+-- Controllers/        < AuthController, CoursesController, SectionsController, LessonsController,
¦                         TestsController, UploadsController, UsersController, CategoriesController
+-- Extensions/         < ResultExtensions (ToActionResult), WebApplicationExtensions
+-- Middleware/         < ExceptionHandlingMiddleware, SecurityHeadersMiddleware
L-- RateLimiting/       < RateLimitPolicies
```

---

