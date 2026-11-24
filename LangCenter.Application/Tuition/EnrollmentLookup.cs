namespace LangCenter.Application.Tuition;

public record EnrollmentLookupDto(Guid EnrollmentId, Guid TrainingClassId, string ClassName, string StudentName);

public interface IEnrollmentLookupQuery
{
    Task<IReadOnlyList<EnrollmentLookupDto>> FindByStudentEmailAsync(string studentEmail, CancellationToken ct = default);
}
