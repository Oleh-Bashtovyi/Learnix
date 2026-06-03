using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Certificates.Specifications;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Settings;
using Learnix.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Infrastructure.Services;

internal sealed class CertificatePdfGenerationService(
    IServiceScopeFactory scopeFactory,
    ILogger<CertificatePdfGenerationService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        await RunAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunAsync(stoppingToken);
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();

            var certRepo = scope.ServiceProvider.GetRequiredService<ICertificateRepository>();
            var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var appSettings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
            var certificateNotifier = scope.ServiceProvider.GetRequiredService<ICertificateNotifier>();

            var pending = await certRepo.ListAsync(new PendingCertificatesSpecification(), ct);
            if (pending.Count == 0) return;

            logger.LogInformation("Certificate generation: {Count} pending.", pending.Count);

            foreach (var cert in pending)
            {
                try
                {
                    var course = await dbContext.Courses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == cert.CourseId, ct);

                    var student = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == cert.StudentId, ct);

                    if (course is null || student is null)
                    {
                        logger.LogWarning("Certificate {CertId}: missing course or student, skipping.", cert.Id);
                        continue;
                    }

                    var instructor = await dbContext.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == course.InstructorId, ct);

                    var enrollment = await dbContext.Enrollments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.Id == cert.EnrollmentId, ct);

                    var pdfBytes = CertificatePdfDocument.Generate(new CertificateDocumentData(
                        StudentFullName: $"{student.FirstName} {student.LastName}",
                        CourseTitle: course.Title,
                        InstructorName: instructor is not null
                            ? $"{instructor.FirstName} {instructor.LastName}"
                            : "Learnix Instructor",
                        CompletedAt: enrollment?.CompletedAt ?? cert.IssuedAt,
                        Code: cert.Code,
                        VerificationUrl: $"{appSettings.ClientBaseUrl}/verify/{cert.Code}"));

                    var blobPath = $"certificates/{cert.Code}.pdf";
                    using var stream = new MemoryStream(pdfBytes);
                    await blobService.UploadAsync(blobPath, stream, "application/pdf", ct);

                    // Re-fetch tracked to update
                    var trackedCert = await dbContext.Certificates.FindAsync([cert.Id], ct);
                    trackedCert!.AttachFile(blobPath);
                    await dbContext.SaveChangesAsync(ct);

                    await certificateNotifier.NotifyCertificateReadyAsync(
                        cert.StudentId, cert.Id, course.Title, ct);

                    logger.LogInformation("Certificate {Code} generated for student {StudentId}.",
                        cert.Code, cert.StudentId);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogError(ex, "Failed to generate certificate {CertId}.", cert.Id);
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Certificate generation loop failed.");
        }
    }
}
