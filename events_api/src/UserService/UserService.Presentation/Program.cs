using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserService.Application;
using UserService.Application.Interfaces;
using UserService.Application.Settings;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Infrastructure;
using UserService.Infrastructure.DataAccess;
using UserService.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console(new CompactJsonFormatter()));

// 1. Контроллеры
builder.Services.AddControllers();

// 2. Слои Application + Infrastructure
builder.Services.AddUserApplicationServices();
builder.Services.AddUserInfrastructureServices(builder.Configuration);

// 3. JWT Authentication
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSecret = jwtSection["Secret"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // сохраняем "sub" как есть
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// 4. Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "...", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен (без префикса Bearer)"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// 5. OpenTelemetry
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"]
    ?? throw new InvalidOperationException(
        "Otlp:Endpoint не задан в appsettings.json");
var serviceName = builder.Configuration["Otlp:ServiceName"]
    ?? throw new InvalidOperationException(
        "Otlp:ServiceName не задан в appsettings.json");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/metrics");
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(o =>
            o.Endpoint = new Uri(otlpEndpoint)))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// 6. Build
var app = builder.Build();
app.MapPrometheusScrapingEndpoint();

// 7. Миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    db.Database.Migrate();

    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    if (!await userRepo.ExistsByLoginAsync("admin"))
    {
        var admin = new User("admin", hasher.Hash("admin123"), UserRole.Admin);
        await userRepo.AddAsync(admin);
    }
}

app.UseSerilogRequestLogging();

// 8. Пайплайн
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();