using LangCenter.Application.Certificates;

namespace LangCenter.Application.Enrollments;

public record ClassEnrollmentRow(Guid Id, Guid StudentId, string StudentName, string StudentEmail);

public record EnrollmentWorkspaceDto(Guid Id, Guid ClassId, string ClassName, string ClassStatus,
    string StudentName, bool ExamAdmitted, CertificateDto? Certificate);

public interface IEnrollmentWorkspaceQuery
{
    Task<IReadOnlyList<ClassEnrollmentRow>> GetRosterAsync(Guid classId, CancellationToken ct = default);
    Task<EnrollmentWorkspaceDto?> GetDetailsAsync(Guid id, CancellationToken ct = default);
}
