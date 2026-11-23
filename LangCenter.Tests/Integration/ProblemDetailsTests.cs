using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LangCenter.Application.Common;
using LangCenter.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LangCenter.Tests.Integration;

public class ProblemDetailsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData(400, "Class has not reached the minimum enrollment.")]
    [InlineData(404, "Class was not found.")]
    [InlineData(409, "The student is already enrolled.")]
    [InlineData(500, "Đã xảy ra lỗi. Vui lòng thử lại sau.")]
    public async Task HandleError_WhenRequestThrows_ReturnsProblemDetails(int status, string detail)
    {
        using var testApp = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ProblemDetailsProbeController).Assembly)));
        using var client = testApp.CreateClient();
        var path = $"/test-errors/{status}";

        using var response = await client.GetAsync(path);

        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.Equal(path, problem.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }
}

// Controller chỉ có trong host kiểm thử để kích hoạt từng nhánh xử lý lỗi.
[ApiController]
[Route("test-errors")]
public class ProblemDetailsProbeController : ControllerBase
{
    [HttpGet("{status:int}")]
    public IActionResult GetError(int status) => throw status switch
    {
        400 => new BusinessRuleException("Class has not reached the minimum enrollment."),
        404 => new NotFoundException("Class was not found."),
        409 => new ConflictException("The student is already enrolled."),
        _ => new InvalidOperationException("Internal test failure details.")
    };
}
