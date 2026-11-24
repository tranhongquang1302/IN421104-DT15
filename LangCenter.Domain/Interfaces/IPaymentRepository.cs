using LangCenter.Domain.Entities;

namespace LangCenter.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<decimal> SumByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<Payment?> GetByRequestIdAsync(Guid enrollmentId, Guid requestId, CancellationToken ct = default);
    void Add(Payment payment);
}
