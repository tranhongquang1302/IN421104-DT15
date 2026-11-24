namespace LangCenter.Domain.Entities;

public class ExamAdmission
{
    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public DateTime AdmittedAt { get; private set; }

    private ExamAdmission() { }

    public static ExamAdmission Create(Enrollment enrollment, ClassStatus classStatus, decimal paidAmount,
        DateTime admittedAt)
    {
        if (classStatus != ClassStatus.Started)
            throw new InvalidOperationException("Lớp chưa khai giảng.");
        if (enrollment.TuitionDue != paidAmount)
            throw new InvalidOperationException("Học viên còn nợ học phí nên chưa đủ điều kiện dự thi.");

        return new ExamAdmission
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            AdmittedAt = admittedAt.AddTicks(-(admittedAt.Ticks % TimeSpan.TicksPerMicrosecond))
        };
    }
}
