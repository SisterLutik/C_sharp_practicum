using events_api.Data;
using events_api.Interfaces;
using events_api.Services;

var builder = WebApplication.CreateBuilder(args);


//Регистрируем зависимости в DI-контейнере
builder.Services.AddScoped<IEventRepository, EventRepository>();

builder.Services.AddOpenApi();

var app = builder.Build();

//  DI автоматически внедряет зависимости
app.MapGet("/events", (EventService eventService) =>
{
    var events = eventService.GetAllEvents();
    return Results.Ok(events);
});

app.MapGet("/events/{id:int}", (int id, EventService eventService) =>
{
    var eventItem = eventService.GetEvent(id);
    return eventItem != null ? Results.Ok(eventItem) : Results.NotFound();
});

app.MapPost("/events", (CreateEventRequest request, EventService eventService) =>
{
    eventService.CreateOrder(request.CustomerName, request.TotalAmount);
    return Results.Ok(new { message = "Заказ создан" });
});

app.MapPost("/events/{id:int}/confirm", (int id, EventService eventService) =>
{
    eventService.ConfirmOrder(id);
    return Results.Ok(new { message = "Заказ подтверждён" });
});

app.Run();

record CreateEventRequest(string CustomerName, decimal TotalAmount);
