using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LangCenter.Tests.Integration;

public class PostgresPortalTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    [PostgresFact]
    public async Task CompleteCourse_UsingPostgres_PersistsAndVerifiesCertificate()
    {
        using var registrar = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var cashier = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var visitor = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await PortalForms.LoginAsync(registrar, AuthenticatedApiFactory.RegistrarEmail);
        await PortalForms.LoginAsync(cashier, AuthenticatedApiFactory.CashierEmail);
        await PortalWorkflow.ExerciseAsync(registrar, cashier, visitor);
    }
}
