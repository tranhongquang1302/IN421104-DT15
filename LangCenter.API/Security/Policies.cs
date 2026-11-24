using LangCenter.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace LangCenter.API.Security;

public static class Policies
{
    public const string ManageStudents = "ManageStudents";
    public const string ManageClasses = "ManageClasses";
    public const string ManageTuition = "ManageTuition";
    public const string ReadTuitionBalance = "ReadTuitionBalance";
    public const string ConfirmExamAdmission = "ConfirmExamAdmission";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(ManageStudents, policy => policy.RequireRole(Roles.Admin, Roles.Registrar));
        options.AddPolicy(ManageClasses, policy => policy.RequireRole(Roles.Admin, Roles.Registrar));
        options.AddPolicy(ManageTuition, policy => policy.RequireRole(Roles.Admin, Roles.Cashier));
        options.AddPolicy(ReadTuitionBalance, policy => policy.RequireRole(Roles.Admin, Roles.Registrar, Roles.Cashier));
        options.AddPolicy(ConfirmExamAdmission, policy => policy.RequireRole(Roles.Admin, Roles.Registrar));
    }
}
