using EventsApi.Application;
using EventsApi.Application.Settings;
using EventsApi.Infrastructure;
using EventsApi.Infrastructure.DataAccess;
using EventsApi.Presentation.BackgroundServices;
using EventsApi.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. РЕГИСТРАЦИЯ СЕРВИСОВ (ДО Build)
// ============================================

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<BookingBackgroundService>();

// JWT Authentication
var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSecret = jwtSection["Secret"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Events API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен"
    });

});

// ============================================
// 2. BUILD
// ============================================
var app = builder.Build();

// ============================================
// 3. MIDDLEWARE (ПОСЛЕ Build)
// ============================================

// Миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Глобальная обработка исключений — первой
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Swagger — только в Development
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