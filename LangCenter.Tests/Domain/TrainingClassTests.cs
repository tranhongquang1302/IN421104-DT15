using LangCenter.Domain.Entities;
using Xunit;

namespace LangCenter.Tests.Domain;

public class TrainingClassTests
{
    private static readonly DateTime StartTime = new(2026, 11, 23, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Launch_BelowMinimumEnrollment_LeavesClassOpen()
    {
        var trainingClass = TrainingClass.Create("Tiếng Anh A1", 2, 1_000_000m);

        Assert.Throws<InvalidOperationException>(() => trainingClass.Launch(1, StartTime));

        Assert.Equal(ClassStatus.Open, trainingClass.Status);
        Assert.Null(trainingClass.StartedAt);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Launch_AtOrAboveMinimumEnrollment_StoresStartTimeAtMicrosecondPrecision(int enrollmentCount)
    {
        var trainingClass = TrainingClass.Create("Tiếng Anh A1", 2, 1_000_000m);

        trainingClass.Launch(enrollmentCount, StartTime.AddTicks(7));

        Assert.Equal(ClassStatus.Started, trainingClass.Status);
        Assert.Equal(StartTime, trainingClass.StartedAt);
    }

    [Fact]
    public void Launch_WhenAlreadyStarted_KeepsOriginalStartTime()
    {
        var trainingClass = TrainingClass.Create("Tiếng Anh A1", 1, 1_000_000m);
        trainingClass.Launch(1, StartTime);

        Assert.Throws<InvalidOperationException>(() => trainingClass.Launch(1, StartTime.AddHours(1)));

        Assert.Equal(StartTime, trainingClass.StartedAt);
    }
}
