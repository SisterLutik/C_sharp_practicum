using events_api.Interfaces;
using events_api.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры
builder.Services.AddControllers();

// Добавляем Swagger
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

// ✅ Регистрация EventService как Singleton (для in-memory хранилища)
builder.Services.AddSingleton<IEventService, EventService>();

var app = builder.Build();

// Настройка Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Перенаправление с корня на Swagger
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();