namespace LangCenter.Domain.Entities;

public class Certificate
{
    public const int LookupCodeLength = 32;

    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public string LookupCode { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }

    private Certificate() { }

    public static Certificate Create(Enrollment enrollment, bool completionConfirmed, bool examAdmissionExists,
        string lookupCode, DateTime issuedAt)
    {
        if (!completionConfirmed)
            throw new InvalidOperationException("Giáo vụ phải xác nhận học viên đã hoàn thành khóa học.");
        if (!examAdmissionExists)
            throw new InvalidOperationException("Lượt ghi danh chưa được xác nhận đủ điều kiện dự thi.");
        if (!IsValidLookupCode(lookupCode))
            throw new ArgumentException("Mã tra cứu chứng chỉ không hợp lệ.", nameof(lookupCode));

        return new Certificate
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollment.Id,
            LookupCode = lookupCode.ToUpperInvariant(),
            IssuedAt = issuedAt.AddTicks(-(issuedAt.Ticks % TimeSpan.TicksPerMicrosecond))
        };
    }

    public static bool IsValidLookupCode(string? lookupCode) =>
        lookupCode is { Length: LookupCodeLength } && lookupCode.All(Uri.IsHexDigit);
}
