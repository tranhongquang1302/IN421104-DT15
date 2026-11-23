namespace LangCenter.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; private set; }
    public Guid StudentId { get; private set; }
    public Guid TrainingClassId { get; private set; }
    public decimal TuitionDue { get; private set; }
    public DateTime EnrolledAt { get; private set; }

    private Enrollment() { }

    public static Enrollment Create(Guid studentId, TrainingClass trainingClass, DateTime enrolledAt)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Phải xác định học viên để ghi danh.", nameof(studentId));
        if (trainingClass.Status != ClassStatus.Open)
            throw new InvalidOperationException("Lớp đã khai giảng và đã dừng nhận ghi danh.");

        return new Enrollment
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            TrainingClassId = trainingClass.Id,
            TuitionDue = trainingClass.TuitionFee,
            EnrolledAt = enrolledAt.AddTicks(-(enrolledAt.Ticks % TimeSpan.TicksPerMicrosecond))
        };
    }
}
