using LangCenter.Application.Common;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Classes;

public record LaunchClassCommand(Guid Id);

public class LaunchClassCommandHandler(ITrainingClassRepository classes, IEnrollmentRepository enrollments,
    IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<TrainingClassDto> HandleAsync(LaunchClassCommand command, CancellationToken ct = default) =>
        unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var trainingClass = await classes.GetByIdForUpdateAsync(command.Id, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lớp học.");

            var enrollmentCount = await enrollments.CountByClassAsync(trainingClass.Id, transactionCt);
            try
            {
                trainingClass.Launch(enrollmentCount, clock.GetUtcNow().UtcDateTime);
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            return TrainingClassDto.FromTrainingClass(trainingClass);
        }, ct);
}
