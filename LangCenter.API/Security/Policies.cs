using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace LangCenter.API.Security;

public static class Policies
{
    public const string ManageStudents = "ManageStudents";
    public const string ManageClasses = "ManageClasses";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageStudents, policy => policy.RequireRole(Roles.Admin, Roles.Registrar));
        options.AddPolicy(ManageClasses, policy => policy.RequireRole(Roles.Admin, Roles.Registrar));
    }
}
