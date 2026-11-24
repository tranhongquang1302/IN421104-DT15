using LangCenter.Domain.Entities;

namespace LangCenter.Application.Tuition;

public record PaymentDto(Guid Id, Guid EnrollmentId, decimal Amount, Guid? RequestId, DateTime PaidAt)
{
    public static PaymentDto FromPayment(Payment payment) =>
        new(payment.Id, payment.EnrollmentId, payment.Amount, payment.RequestId, payment.PaidAt);
}
