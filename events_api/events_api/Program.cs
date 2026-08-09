using events_api.DataAccess;
using events_api.Interfaces;
using events_api.Middleware;
using events_api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Добавляем контроллеры
builder.Services.AddControllers();

// 2. Регистрация DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Регистрация сервисов (Scoped)
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// 4. Фоновый сервис
builder.Services.AddHostedService<BookingBackgroundService>();

// 5. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Events API",
        Version = "v1",
        Description = "API для управления событиями"
    });
});

var app = builder.Build();

// ✅ 6. Создание схемы базы данных
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 7. Middleware
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 8. Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

// 9. Маршрутизация
app.UseHttpsRedirection();
app.MapControllers();

app.Run();