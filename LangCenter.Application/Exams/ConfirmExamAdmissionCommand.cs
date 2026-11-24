using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Exams;

public record ConfirmExamAdmissionCommand(Guid EnrollmentId);

public record ExamAdmissionDto(Guid Id, Guid EnrollmentId, DateTime AdmittedAt)
{
    public static ExamAdmissionDto FromExamAdmission(ExamAdmission admission) =>
        new(admission.Id, admission.EnrollmentId, admission.AdmittedAt);
}

public class ConfirmExamAdmissionCommandHandler(IEnrollmentRepository enrollments, ITrainingClassRepository classes,
    IPaymentRepository payments, IExamAdmissionRepository admissions, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<ExamAdmissionDto> HandleAsync(ConfirmExamAdmissionCommand command, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var enrollment = await enrollments.GetByIdForUpdateAsync(command.EnrollmentId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lượt ghi danh.");
            if (await admissions.ExistsAsync(enrollment.Id, transactionCt))
                throw new ConflictException("Lượt ghi danh đã được xác nhận đủ điều kiện dự thi.");

            var trainingClass = await classes.GetByIdAsync(enrollment.TrainingClassId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lớp học.");
            var paidAmount = await payments.SumByEnrollmentAsync(enrollment.Id, transactionCt);

            ExamAdmission admission;
            try
            {
                admission = ExamAdmission.Create(enrollment, trainingClass.Status, paidAmount, clock.GetUtcNow().UtcDateTime);
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            admissions.Add(admission);
            return ExamAdmissionDto.FromExamAdmission(admission);
        }, ct);
}
