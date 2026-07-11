using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Domain.UnitTests.Entities;

public class PaymentTests
{
    [Fact]
    public void CreateMock_ShouldRecordACompletedUsdPaymentAgainstTheMockProvider()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();

        // Act
        var payment = Payment.CreateMock(userId, courseId, enrollmentId, amount: 49.99m);

        // Assert — there is no real gateway: a mock payment is born already settled. If a real
        // provider is ever wired in, this test is the reminder that the defaults must change.
        payment.UserId.Should().Be(userId);
        payment.CourseId.Should().Be(courseId);
        payment.EnrollmentId.Should().Be(enrollmentId);
        payment.Amount.Should().Be(49.99m);
        payment.Currency.Should().Be("USD");
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.PaymentProvider.Should().Be("Mock");
        payment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
