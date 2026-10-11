using EventService.Application;
using EventService.Infrastructure;
using EventService.Infrastructure.DataAccess;
using EventService.Presentation.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using EventService.Infrastructure.Kafka;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 1. Контроллеры
// ============================================
builder.Services.AddControllers();

// ============================================
// 2. Слои Application + Infrastructure
// ============================================
builder.Services.AddEventApplicationServices();
builder.Services.AddEventInfrastructureServices(builder.Configuration);

//  инициализатор топика
builder.Services.AddHostedService<KafkaTopicInitializer>();

// ✅ Фоновый подписчик на Kafka
builder.Services.AddHostedService<BookingConfirmedConsumer>();

// ============================================
// 3. JWT Authentication
// ============================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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

// ============================================
// 4. Swagger + JWT
// ============================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Service API",
        Version = "v1",
        Description = "Сервис событий: CRUD и учёт доступных мест"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT-токен (без префикса Bearer)"
    });
});

// ============================================
// 5. Build
// ============================================
var app = builder.Build();

// ============================================
// 6. Миграции
// ============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.Migrate();
}

// ============================================
// 7. Пайплайн
// ============================================
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