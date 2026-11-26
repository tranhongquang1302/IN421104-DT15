using LangCenter.Domain.Entities;
using Xunit;

namespace LangCenter.Tests.Domain;

public class ClassSessionTests
{
    private static readonly DateTime Start = new(2026, 11, 23, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidTimeAndResources_CreatesSession()
    {
        var classId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var end = Start.AddHours(1.5);

        var session = ClassSession.Create(classId, roomId, instructorId, Start, end);

        Assert.NotEqual(Guid.Empty, session.Id);
        Assert.Equal(classId, session.TrainingClassId);
        Assert.Equal(roomId, session.RoomId);
        Assert.Equal(instructorId, session.InstructorId);
        Assert.Equal(Start, session.StartsAt);
        Assert.Equal(end, session.EndsAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Reschedule_EndIsNotAfterStart_LeavesOriginalSession(int minutes)
    {
        var roomId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();
        var session = ClassSession.Create(Guid.NewGuid(), roomId, instructorId, Start, Start.AddHours(1));

        Assert.Throws<ArgumentException>(() =>
            session.Reschedule(Guid.NewGuid(), Guid.NewGuid(), Start, Start.AddMinutes(minutes)));

        Assert.Equal(roomId, session.RoomId);
        Assert.Equal(instructorId, session.InstructorId);
        Assert.Equal(Start, session.StartsAt);
        Assert.Equal(Start.AddHours(1), session.EndsAt);
    }
}
