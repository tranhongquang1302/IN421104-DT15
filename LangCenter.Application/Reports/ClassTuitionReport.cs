using LangCenter.Application.Common;

namespace LangCenter.Application.Reports;

public record ClassTuitionReportRowDto(Guid EnrollmentId, string StudentName, decimal TuitionDue, decimal PaidAmount)
{
    public decimal OutstandingAmount => TuitionDue - PaidAmount;
}

public record ClassTuitionReportDto(Guid TrainingClassId, string ClassName, IReadOnlyList<ClassTuitionReportRowDto> Enrollments)
{
    public decimal TuitionDue => Enrollments.Sum(row => row.TuitionDue);
    public decimal PaidAmount => Enrollments.Sum(row => row.PaidAmount);
    public decimal OutstandingAmount => TuitionDue - PaidAmount;
}

public record PagedClassTuitionReportDto(Guid TrainingClassId, string ClassName,
    PagedResult<ClassTuitionReportRowDto> Enrollments, decimal TuitionDue, decimal PaidAmount)
{
    public decimal OutstandingAmount => TuitionDue - PaidAmount;
}

public interface IClassTuitionReportQuery
{
    Task<ClassTuitionReportDto?> GetByClassIdAsync(Guid classId, CancellationToken ct = default);
    Task<PagedClassTuitionReportDto?> GetPageAsync(Guid classId, PageRequest paging, CancellationToken ct = default);
}
