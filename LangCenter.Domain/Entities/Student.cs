using System.Net.Mail;

namespace LangCenter.Domain.Entities;

public class Student
{
    public const int MaxFullNameLength = 150;
    public const int MaxEmailLength = 254;

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    private Student() { }

    public static Student Create(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > MaxFullNameLength)
            throw new ArgumentException($"Họ tên học viên phải có từ 1 đến {MaxFullNameLength} ký tự.", nameof(fullName));

        var address = email?.Trim();
        if (string.IsNullOrWhiteSpace(address) || address.Length > MaxEmailLength ||
            !MailAddress.TryCreate(address, out var parsed) || parsed.Address != address)
            throw new ArgumentException("Email học viên không hợp lệ.", nameof(email));

        return new Student
        {
            Id = Guid.NewGuid(),
            FullName = fullName.Trim(),
            Email = address.ToLowerInvariant()
        };
    }
}
