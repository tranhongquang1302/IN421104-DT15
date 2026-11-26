namespace LangCenter.Domain.Entities;

public class ClassSession
{
    public Guid Id { get; private set; }
    public Guid TrainingClassId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid InstructorId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime EndsAt { get; private set; }

    private ClassSession() { }

    public static ClassSession Create(Guid trainingClassId, Guid roomId, Guid instructorId, DateTime startsAt, DateTime endsAt)
    {
        if (trainingClassId == Guid.Empty)
            throw new ArgumentException("Hãy chọn lớp học.", nameof(trainingClassId));

        var session = new ClassSession { Id = Guid.NewGuid(), TrainingClassId = trainingClassId };
        session.Reschedule(roomId, instructorId, startsAt, endsAt);
        return session;
    }

    public void Reschedule(Guid roomId, Guid instructorId, DateTime startsAt, DateTime endsAt)
    {
        if (roomId == Guid.Empty)
            throw new ArgumentException("Hãy chọn phòng học.", nameof(roomId));
        if (instructorId == Guid.Empty)
            throw new ArgumentException("Hãy chọn giảng viên.", nameof(instructorId));
        if (startsAt.Kind != DateTimeKind.Utc || endsAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Thời gian lưu buổi học phải dùng UTC.");

        var start = startsAt.AddTicks(-(startsAt.Ticks % TimeSpan.TicksPerMicrosecond));
        var end = endsAt.AddTicks(-(endsAt.Ticks % TimeSpan.TicksPerMicrosecond));
        if (end <= start)
            throw new ArgumentException("Giờ kết thúc phải sau giờ bắt đầu.", nameof(endsAt));

        RoomId = roomId;
        InstructorId = instructorId;
        StartsAt = start;
        EndsAt = end;
    }
}
