Events API

API для управления событиями и бронированиями.

Требования

- .NET 10.0 (Preview) или выше
- PostgreSQL
- Docker (для запуска интеграционных тестов)
- Любая ОС (Windows, Linux, macOS)

Структура проекта

Проект разделён на четыре слоя в соответствии с принципами чистой архитектуры. Каждый слой — отдельная сборка (class library), что гарантирует соблюдение направления зависимостей на уровне компилятора.

events_api/
├── events_api/                             Корневая папка решения
│   ├── events_api.sln                      Файл решения
│   ├── src/
│   │   ├── EventsApi.Domain/               Доменный слой
│   │   │   └── EventsApi.Domain.csproj
│   │   ├── EventsApi.Application/          Слой приложения
│   │   │   └── EventsApi.Application.csproj
│   │   ├── EventsApi.Infrastructure/       Инфраструктурный слой
│   │   │   └── EventsApi.Infrastructure.csproj
│   │   └── EventsApi.Presentation/         Слой представления (точка входа, Web API)
│   │       └── EventsApi.Presentation.csproj
│   ├── events_api.Tests/                   Юнит-тесты
│   │   └── events_api.Tests.csproj
│   └── events_api.IntegrationTests/        Интеграционные тесты
│       └── events_api.IntegrationTests.csproj

Назначение слоёв

EventsApi.Domain — доменные сущности (Event, Booking), перечисления (BookingStatus), доменные исключения (BusinessException, NoAvailableSeatsException). Не зависит ни от каких внешних библиотек и фреймворков.

EventsApi.Application — бизнес-логика (use cases), интерфейсы сервисов (IEventService, IBookingService), интерфейсы портов для доступа к данным (IEventRepository, IBookingRepository), DTO (EventResponse, BookingResponse, CreateEventRequest, PaginatedResult). Зависит только от Domain.

EventsApi.Infrastructure — реализации портов: репозитории (EventRepository, BookingRepository), AppDbContext, конфигурации маппинга, миграции. Зависит от Application и Domain.

EventsApi.Presentation — контроллеры, middleware для обработки исключений, фоновая служба, Program.cs (composition root), настройка Swagger. Зависит от Application и Infrastructure.

Схема зависимостей

Presentation → Application + Infrastructure
Infrastructure → Application + Domain
Application → Domain
Domain → (ни от чего не зависит)

Компилятор не позволит нарушить это направление: если Domain попытается сослаться на Infrastructure, сборка упадёт.

Пути к проектам

src/EventsApi.Domain/EventsApi.Domain.csproj
src/EventsApi.Application/EventsApi.Application.csproj
src/EventsApi.Infrastructure/EventsApi.Infrastructure.csproj
src/EventsApi.Presentation/EventsApi.Presentation.csproj
events_api.Tests/events_api.Tests.csproj
events_api.IntegrationTests/events_api.IntegrationTests.csproj

Настройка базы данных

1. Установите PostgreSQL.
2. Создайте базу данных eventapi.
3. В файле src/EventsApi.Presentation/appsettings.json укажите строку подключения:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=ваш_пароль"
  }
}

Миграции

Схема базы данных управляется через миграции Entity Framework Core. Все миграции находятся в проекте EventsApi.Infrastructure. При запуске приложения миграции применяются автоматически через db.Database.Migrate() в Program.cs.

Создание новой миграции

dotnet ef migrations add <Name> --project events_api/src/EventsApi.Infrastructure --startup-project events_api/src/EventsApi.Presentation

Запуск приложения

1. Восстановите зависимости:
dotnet restore events_api/events_api.sln

2. Запустите приложение (точка входа — EventsApi.Presentation):
dotnet run --project events_api/src/EventsApi.Presentation

3. Откройте Swagger UI:
http://localhost:5286/swagger (профиль http)
https://localhost:7058/swagger (профиль https)

Запуск тестов

Все тесты (юнит и интеграционные) через решение:

dotnet test events_api/events_api.sln

Юнит-тесты

Используют InMemory-провайдер EF Core, не требуют базы данных и Docker.

dotnet test events_api/events_api.Tests/events_api.Tests.csproj

Интеграционные тесты

Используют Testcontainers и требуют запущенный Docker. Перед запуском убедитесь, что Docker Desktop запущен.

dotnet test events_api/events_api.IntegrationTests/events_api.IntegrationTests.csproj

Эндпоинты

События

GET /api/events — получить все события с фильтрацией и пагинацией
GET /api/events/{id} — получить событие по ID
POST /api/events — создать событие
PUT /api/events/{id} — полностью обновить событие
DELETE /api/events/{id} — удалить событие

Бронирования

POST /api/events/{id}/book — создать бронь для события
GET /api/bookings/{id} — получить бронь по ID

Модели данных

Event

id (Guid) — уникальный идентификатор события
title (string) — название события
description (string?) — описание события
startAt (DateTime) — дата и время начала
endAt (DateTime) — дата и время окончания
totalSeats (int) — общее количество мест
availableSeats (int) — текущее количество свободных мест

Booking

id (Guid) — уникальный идентификатор брони
eventId (Guid) — идентификатор события
status (BookingStatus) — текущий статус брони
createdAt (DateTime) — дата и время создания
processedAt (DateTime?) — дата и время обработки

BookingStatus (enum)

Pending — бронь создана, ожидает обработки
Confirmed — бронь подтверждена
Rejected — бронь отклонена

Параметры запроса (GET /api/events)

title (string) — поиск по названию (регистронезависимый, частичное совпадение)
from (DateTime) — события, начинающиеся не раньше даты
to (DateTime) — события, заканчивающиеся не позже даты
page (int) — номер страницы (по умолчанию 1)
pageSize (int) — количество элементов на странице (по умолчанию 10)

Формат ответа (GET /api/events)

{
    "page": 1,
    "pageSize": 10,
    "totalCount": 2,
    "totalPages": 1,
    "items": [
        {
            "id": "3f2c1d8e-1234-5678-9abc-def012345678",
            "title": "Конференция по IT",
            "description": "Описание конференции",
            "startAt": "2025-07-05T10:00:00",
            "endAt": "2025-07-05T18:00:00",
            "totalSeats": 50,
            "availableSeats": 45
        }
    ]
}

Фоновая обработка броней

Приложение содержит фоновый сервис BookingBackgroundService, который автоматически обрабатывает бронирования. Сама логика обработки одной брони (Confirm/Reject) вынесена в слой Application (BookingProcessor).

- Интервал опроса: 5 секунд
- Проверка: ищет брони со статусом Pending
- Обработка: параллельная (Task.WhenAll), для каждой брони выполняется задержка 2 секунды (имитация внешней системы)
- Результат: бронь переводится в статус Confirmed
- Заполнение: поле processedAt получает текущую дату и время
- При ошибке или удалении события: бронь переводится в статус Rejected

Примеры запросов

POST /api/events

{
    "title": "Новая конференция",
    "description": "Описание конференции",
    "startAt": "2025-07-01T10:00:00",
    "endAt": "2025-07-01T18:00:00",
    "totalSeats": 50
}

POST /api/events/{id}/book

Запрос: тело пустое.

Заголовки ответа:
Location: /api/bookings/3f2c1d8e-1234-5678-9abc-def012345678

Ответ (202 Accepted):
{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "eventId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "status": 0,
    "createdAt": "2025-07-15T10:30:00.123Z",
    "processedAt": null
}

Ответ (409 Conflict) — при отсутствии свободных мест:
{
    "status": 409,
    "detail": "No available seats for this event"
}

GET /api/bookings/{id}

Ответ (200 OK):
{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "eventId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "status": 1,
    "createdAt": "2025-07-15T10:30:00.123Z",
    "processedAt": "2025-07-15T10:30:15.456Z"
}

PUT /api/events/{id}

{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "title": "Обновлённое название",
    "description": "Новое описание",
    "startAt": "2025-08-01T10:00:00",
    "endAt": "2025-08-01T20:00:00",
    "totalSeats": 30,
    "availableSeats": 25
}

DELETE /api/events/{id}

Ответ: 204 No Content

Коды ответов

200 — успешно
201 — создано
202 — принято в обработку
204 — удалено
400 — ошибка валидации
404 — ресурс не найден
409 — конфликт (нет свободных мест)
500 — внутренняя ошибка сервера

Валидация

Title — обязательное
StartAt — обязательное
EndAt — обязательное, должно быть позже StartAt
TotalSeats — обязательное, должно быть больше 0

Технологии

- ASP.NET Core 10.0 (Preview)
- Entity Framework Core
- PostgreSQL
- Testcontainers
- Swagger / OpenAPI
- xUnit (тесты)
- C# 13.0