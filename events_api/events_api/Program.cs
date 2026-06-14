using events_api.Interfaces;
using events_api.Services;
using Microsoft.OpenApi;


var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddControllers();
builder.Services.AddScoped<IEventService, EventService>();

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

// ✅ Регистрация Dependency Injection
builder.Services.AddScoped<IEventService, EventService>();

var app = builder.Build();

// Настройка конвейера запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger"));

}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();