using LangCenter.Domain.Entities;
using Xunit;

namespace LangCenter.Tests.Domain;

public class ExamAdmissionTests
{
    private const decimal TuitionFee = 1_200_000m;
    private static readonly DateTime AdmissionTime = new(2026, 11, 23, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0)]
    [InlineData(1_200_000)]
    public void Create_WhenClassHasStartedAndBalanceIsZero_ReturnsAdmission(int tuitionFee)
    {
        // Arrange
        var enrollment = CreateEnrollment(tuitionFee);

        // Act
        var admission = ExamAdmission.Create(enrollment, ClassStatus.Started, tuitionFee, AdmissionTime.AddTicks(7));

        // Assert
        Assert.NotEqual(Guid.Empty, admission.Id);
        Assert.Equal(enrollment.Id, admission.EnrollmentId);
        Assert.Equal(AdmissionTime, admission.AdmittedAt);
        Assert.Equal(DateTimeKind.Utc, admission.AdmittedAt.Kind);
    }

    [Fact]
    public void Create_WhenClassIsOpenAndTuitionIsPaid_ThrowsInvalidOperationException()
    {
        // Arrange
        var enrollment = CreateEnrollment(TuitionFee);

        // Act
        var act = () => ExamAdmission.Create(enrollment, ClassStatus.Open, TuitionFee, AdmissionTime);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(600_000)]
    [InlineData(1_200_001)]
    public void Create_WhenPaidAmountDiffersFromTuition_ThrowsInvalidOperationException(int paidAmount)
    {
        // Arrange
        var enrollment = CreateEnrollment(TuitionFee);

        // Act
        var act = () => ExamAdmission.Create(enrollment, ClassStatus.Started, paidAmount, AdmissionTime);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    private static Enrollment CreateEnrollment(decimal tuitionFee)
    {
        var trainingClass = TrainingClass.Create("English A1", 1, tuitionFee);
        return Enrollment.Create(Guid.NewGuid(), trainingClass, AdmissionTime.AddDays(-7));
    }
}
