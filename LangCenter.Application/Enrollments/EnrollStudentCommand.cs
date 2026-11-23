using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Enrollments;

public record EnrollStudentCommand(Guid ClassId, Guid StudentId);

public class EnrollStudentCommandHandler(ITrainingClassRepository classes, IStudentRepository students,
    IEnrollmentRepository enrollments, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<EnrollmentDto> HandleAsync(EnrollStudentCommand command, CancellationToken ct = default)
    {
        if (command.StudentId == Guid.Empty)
            throw new BusinessRuleException("Phải xác định học viên để ghi danh.");

        return unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var trainingClass = await classes.GetByIdForUpdateAsync(command.ClassId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lớp học.");
            _ = await students.GetByIdAsync(command.StudentId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy học viên.");

            Enrollment enrollment;
            try
            {
                enrollment = Enrollment.Create(command.StudentId, trainingClass, clock.GetUtcNow().UtcDateTime);
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            if (await enrollments.ExistsAsync(command.ClassId, command.StudentId, transactionCt))
                throw new ConflictException("Học viên đã ghi danh vào lớp này.");

            enrollments.Add(enrollment);
            return EnrollmentDto.FromEnrollment(enrollment);
        }, ct);
    }
}
