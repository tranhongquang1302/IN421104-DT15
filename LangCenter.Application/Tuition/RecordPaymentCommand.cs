using LangCenter.Application.Common;
using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;

namespace LangCenter.Application.Tuition;

public record RecordPaymentCommand(Guid EnrollmentId, decimal Amount, Guid? RequestId);

public class RecordPaymentCommandHandler(IEnrollmentRepository enrollments, IPaymentRepository payments,
    IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<PaymentDto> HandleAsync(RecordPaymentCommand command, CancellationToken ct = default)
    {
        Payment payment;
        try
        {
            payment = Payment.Create(command.EnrollmentId, command.Amount, command.RequestId, clock.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            var enrollment = await enrollments.GetByIdForUpdateAsync(command.EnrollmentId, transactionCt)
                ?? throw new NotFoundException("Không tìm thấy lượt ghi danh.");

            if (command.RequestId is Guid requestId)
            {
                var existing = await payments.GetByRequestIdAsync(enrollment.Id, requestId, transactionCt);
                if (existing is not null)
                {
                    if (existing.Amount != payment.Amount)
                        throw new ConflictException("Mã yêu cầu thanh toán đã được sử dụng với số tiền khác.");
                    return PaymentDto.FromPayment(existing);
                }
            }

            var paidAmount = await payments.SumByEnrollmentAsync(enrollment.Id, transactionCt);
            if (payment.Amount > enrollment.TuitionDue - paidAmount)
                throw new BusinessRuleException("Khoản thanh toán vượt quá học phí còn nợ.");

            payments.Add(payment);
            return PaymentDto.FromPayment(payment);
        }, ct);
    }
}
