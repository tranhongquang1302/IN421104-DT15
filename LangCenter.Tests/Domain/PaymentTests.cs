using LangCenter.Domain.Entities;
using Xunit;

namespace LangCenter.Tests.Domain;

public class PaymentTests
{
    private static readonly DateTime PaymentTime = new(2026, 11, 23, 8, 0, 0, DateTimeKind.Utc);

    public static TheoryData<decimal> BoundaryAmounts => new()
    {
        1m,
        TrainingClass.MaxTuitionFee
    };

    public static TheoryData<decimal> InvalidAmounts => new()
    {
        0m,
        -1m,
        1.5m,
        TrainingClass.MaxTuitionFee + 1m
    };

    [Fact]
    public void Create_WithValidData_ReturnsPaymentWithExpectedValues()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        const decimal amount = 1_200_000m;

        // Act
        var payment = Payment.Create(enrollmentId, amount, requestId, PaymentTime.AddTicks(7));

        // Assert
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(enrollmentId, payment.EnrollmentId);
        Assert.Equal(amount, payment.Amount);
        Assert.Equal(requestId, payment.RequestId);
        Assert.Equal(PaymentTime, payment.PaidAt);
        Assert.Equal(DateTimeKind.Utc, payment.PaidAt.Kind);
    }

    [Theory]
    [MemberData(nameof(BoundaryAmounts))]
    public void Create_WithoutRequestId_AcceptsBoundaryAmounts(decimal amount)
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();

        // Act
        var payment = Payment.Create(enrollmentId, amount, null, PaymentTime);

        // Assert
        Assert.Equal(amount, payment.Amount);
        Assert.Null(payment.RequestId);
    }

    [Theory]
    [MemberData(nameof(InvalidAmounts))]
    public void Create_WithInvalidAmount_ThrowsArgumentOutOfRangeException(decimal amount)
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();

        // Act
        var act = () => Payment.Create(enrollmentId, amount, null, PaymentTime);

        // Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyEnrollmentId_ThrowsArgumentException()
    {
        // Arrange
        var enrollmentId = Guid.Empty;

        // Act
        var act = () => Payment.Create(enrollmentId, 1_200_000m, null, PaymentTime);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Equal(nameof(enrollmentId), exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyRequestId_ThrowsArgumentException()
    {
        // Arrange
        var enrollmentId = Guid.NewGuid();
        var requestId = Guid.Empty;

        // Act
        var act = () => Payment.Create(enrollmentId, 1_200_000m, requestId, PaymentTime);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Equal(nameof(requestId), exception.ParamName);
    }
}
