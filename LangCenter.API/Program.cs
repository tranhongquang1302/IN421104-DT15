using System.Threading.RateLimiting;
using LangCenter.API.Middleware;
using LangCenter.API.Security;
using LangCenter.Application.Auth;
using LangCenter.Infrastructure.Auth;
using LangCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "LangCenter API", Version = "v1" });
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddDbContext<LangCenterDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection to use PostgreSQL.");

    options.UseNpgsql(connectionString);
});

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
})
    .AddRoles<IdentityRole<Guid>>()
    .AddSignInManager()
    .AddEntityFrameworkStores<LangCenterDbContext>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, IdentityArgon2Hasher>();
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddSingleton(TimeProvider.System);
JwtAuthentication.Configure(builder.Services);
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();
_ = app.Services.GetRequiredService<JwtSettings>();
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var services = scope.ServiceProvider;
    await services.GetRequiredService<LangCenterDbContext>().Database.MigrateAsync();

    var demoPassword = app.Configuration["DevelopmentAccounts:Password"];
    if (!string.IsNullOrWhiteSpace(demoPassword))
        await services.GetRequiredService<DbSeeder>().SeedDevelopmentAccountsAsync(demoPassword);
}
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
