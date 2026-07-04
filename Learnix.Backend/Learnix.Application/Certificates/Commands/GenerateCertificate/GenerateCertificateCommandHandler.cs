using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Constants;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Settings;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.Certificates.Commands.GenerateCertificate;

public sealed class GenerateCertificateCommandHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ICertificateRepository certificateRepository,
    ICourseRepository courseRepository,
    IUserRepository userRepository,
    ICertificatePdfGenerator pdfGenerator,
    IBlobStorageService blobStorageService,
    IUnitOfWork unitOfWork,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<GenerateCertificateCommand, Result<string>>
{
    public async Task<Result<string>> Handle(
        GenerateCertificateCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        // Check if student completed the course via Enrollment status
        var enrollment = await enrollmentRepository.FirstOrDefaultAsync(
            new EnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (enrollment is null || enrollment.Status != EnrollmentStatus.Completed)
            return Result.Fail(new ForbiddenError(CertificateMessages.MustCompleteCourseFirst));

        // Get the Certificate record that was created when they finished the course
        var cert = await certificateRepository.FirstOrDefaultAsync(
            new CertificateByCourseAndStudentSpecification(studentId, request.CourseId, forUpdate: true),
            cancellationToken);

        if (cert is null)
            return Result.Fail(new NotFoundError(CertificateMessages.RecordNotFoundContactSupport));

        // Fetch user data needed for PDF
        var course = await courseRepository.GetByIdAsync(request.CourseId, cancellationToken);
        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.GenericCourseNotFound));

        var student = await userRepository.GetByIdAsync(studentId, cancellationToken);
        var instructor = await userRepository.GetByIdAsync(course.InstructorId, cancellationToken);

        if (student is null)
            return Result.Fail(new NotFoundError(CommonMessages.StudentNotFound));

        // Generate the PDF
        var pdfData = new CertificateDocumentData(
            StudentFullName: $"{student.FirstName} {student.LastName}",
            CourseTitle: course.Title,
            InstructorName: instructor is not null ? $"{instructor.FirstName} {instructor.LastName}" : "Learnix Instructor",
            CompletedAt: enrollment.CompletedAt ?? cert.IssuedAt,
            Code: cert.Code,
            VerificationUrl: $"{appSettings.Value.ClientBaseUrl}/verify/{cert.Code}");

        var pdfBytes = pdfGenerator.Generate(pdfData);

        // Upload to Blob Storage
        var blobPath = $"certificates/{cert.Code}.pdf";
        using var stream = new MemoryStream(pdfBytes);
        await blobStorageService.UploadAsync(blobPath, stream, "application/pdf", cancellationToken);

        // Update the database
        cert.AttachFile(blobPath);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Return download URL
        var downloadUrl = blobStorageService.GenerateReadUrl(blobPath, BlobUrlTtlConstants.CertificateReadUrl);
        return Result.Ok(downloadUrl);
    }
}
