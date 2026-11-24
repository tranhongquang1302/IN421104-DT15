using LangCenter.Domain.Entities;
using LangCenter.Domain.Interfaces;
using LangCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LangCenter.Infrastructure.Repositories;

public class PaymentRepository(LangCenterDbContext db) : IPaymentRepository
{
    public Task<decimal> SumByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default) =>
        db.Payments.Where(payment => payment.EnrollmentId == enrollmentId)
            .SumAsync(payment => payment.Amount, ct);

    public Task<Payment?> GetByRequestIdAsync(Guid enrollmentId, Guid requestId, CancellationToken ct = default) =>
        db.Payments.AsNoTracking().FirstOrDefaultAsync(
            payment => payment.EnrollmentId == enrollmentId && payment.RequestId == requestId, ct);

    public void Add(Payment payment) => db.Payments.Add(payment);
}
