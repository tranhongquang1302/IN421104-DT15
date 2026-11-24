using System.Globalization;
using System.Security.Claims;
using LangCenter.Domain.Entities;

namespace LangCenter.API.Pages;

public static class PortalDisplay
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public static string Money(decimal value) => value.ToString("N0", Vietnamese) + " đ";

    public static string ClassStatus(string status) => status == nameof(LangCenter.Domain.Entities.ClassStatus.Started)
        ? "Đã khai giảng"
        : "Đang nhận ghi danh";

    public static string Role(ClaimsPrincipal user) => user.IsInRole(Roles.Admin)
        ? "Quản trị viên"
        : user.IsInRole(Roles.Registrar) ? "Giáo vụ" : "Thu ngân";
}
