using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Serilog;
using SecureFix.Api;
using SecureFix.Core.Data;
using SecureFix.Core.Models;
using SecureFix.Core.Repositories;
using SecureFix.Core.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    DotEnvConfiguration.Load();

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    builder.Services.AddSecureFixAuthentication(builder.Configuration, builder.Environment);

    // Add services to the container
    builder.Services.AddOpenApi();
    builder.Services.AddControllers();

    // Configure database
    var dbPath = builder.Configuration["Database:Path"] ?? "securefix.db";
    builder.Services.AddDbContext<SecureFixDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));

    // Register repositories and unit of work
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Register repository implementations
    builder.Services.AddScoped<IVulnerabilityAlertRepository>(sp =>
        new VulnerabilityAlertRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IRiskAssessmentRepository>(sp =>
        new RiskAssessmentRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IRemediationRecommendationRepository>(sp =>
        new RemediationRecommendationRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IApprovalDecisionRepository>(sp =>
        new ApprovalDecisionRepository(sp.GetRequiredService<SecureFixDbContext>()));
    builder.Services.AddScoped<IAuditEventRepository>(sp =>
        new AuditEventRepository(sp.GetRequiredService<SecureFixDbContext>()));

    // Register business services
    builder.Services.AddSingleton(
        builder.Configuration.GetSection("RiskScoring").Get<RiskScoringPolicy>() ?? new RiskScoringPolicy());
    builder.Services.AddScoped<IRiskScoringEngine, RiskScoringEngine>();
    builder.Services.AddScoped<IAlertIngestionService, AlertIngestionService>();
    builder.Services.AddScoped<IApprovalService, ApprovalService>();
    builder.Services.AddScoped<IDashboardQueryService, DashboardQueryService>();
    builder.Services.AddScoped<IRemediationRecommendationService, RemediationRecommendationService>();
    builder.Services.AddScoped<IPullRequestProposalService, PullRequestProposalService>();
    builder.Services.AddSingleton<IOperationalMetrics, OperationalMetrics>();
    builder.Services.AddSingleton<IKillSwitch, ConfigurationKillSwitch>();

    // Register AI provider factory and provider
    builder.Services.AddScoped<IAIRecommendationProviderFactory, AIRecommendationProviderFactory>();
    builder.Services.AddScoped<IAIRecommendationProvider>(sp =>
    {
        var factory = sp.GetRequiredService<IAIRecommendationProviderFactory>();
        return factory.GetProvider();
    });

    // Register validators
    builder.Services.AddScoped<IValidator<AlertIngestionRequest>, AlertIngestionRequestValidator>();

    // Add CORS with a restricted allow-list for the demo environment.
    builder.Services.AddCors(options =>
    {
        var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000,https://localhost:3000,http://localhost:5173,https://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        options.AddPolicy("SecureFixDemo", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.TryAdd(
                "Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
                "connect-src 'self' https://login.microsoftonline.com; " +
                "frame-src https://login.microsoftonline.com; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");
            return Task.CompletedTask;
        });
        await next();
    });

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SecureFixDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    //app.UseHttpsRedirection();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseCors("SecureFixDemo");
    app.UseAuthentication();
    app.UseAuthorization();

    app.Use(async (context, next) =>
    {
        await next();
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.RequestServices.GetRequiredService<IOperationalMetrics>()
                .RecordRequest(context.Response.StatusCode >= StatusCodes.Status500InternalServerError);
        }
    });

    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("Health");

    // Readiness check endpoint
    app.MapGet("/ready", async (SecureFixDbContext db) =>
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1");
            return Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow });
        }
        catch
        {
            return Results.StatusCode(503);
        }
    })
    .WithName("Ready");

    app.MapGet("/metrics", (IOperationalMetrics metrics) => Results.Ok(metrics.GetSnapshot()))
        .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
        .WithName("Metrics");

    app.MapFallback(async context =>
    {
        var isReservedPath = context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/ready") ||
            context.Request.Path.StartsWithSegments("/metrics") ||
            context.Request.Path.StartsWithSegments("/openapi");
        var indexPath = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");

        if ((!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)) ||
            isReservedPath || !File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
