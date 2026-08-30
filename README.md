Events API

API для управления событиями и бронированиями.

Требования

- .NET 10.0 (Preview) или выше
- PostgreSQL
- Docker (для запуска интеграционных тестов)
- Любая ОС (Windows, Linux, macOS)

Настройка базы данных

1. Установите PostgreSQL на вашем компьютере.
2. Создайте базу данных с именем eventapi.
3. В файле appsettings.json укажите строку подключения:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=ваш_пароль"
  }
}

Миграции

Схема базы данных управляется через Entity Framework Core миграции.

Для создания новой миграции выполните:

dotnet ef migrations add ИмяМиграции

Для применения миграций к базе данных выполните:

dotnet ef database update

При запуске приложения миграции применяются автоматически через метод Migrate().

Запуск проекта

1. Восстановите зависимости:
dotnet restore

2. Запустите приложение:
dotnet run

3. Откройте Swagger UI:
https://localhost:5286/swagger

Запуск тестов

Юнит-тесты используют InMemory-провайдер EF Core и не требуют базы данных.

dotnet test

Интеграционные тесты используют Testcontainers и требуют запущенный Docker. Перед запуском интеграционных тестов убедитесь, что Docker запущен.

cd events_api.IntegrationTests
dotnet test

Эндпоинты

События

GET /api/events - Получить все события с фильтрацией и пагинацией
GET /api/events/{id} - Получить событие по ID
POST /api/events - Создать событие
PUT /api/events/{id} - Полностью обновить событие
DELETE /api/events/{id} - Удалить событие

Бронирования

POST /api/events/{id}/book - Создать бронь для события
GET /api/bookings/{id} - Получить бронь по ID

Модели данных

Event

id (Guid) - Уникальный идентификатор события
title (string) - Название события
description (string?) - Описание события
startAt (DateTime) - Дата и время начала
endAt (DateTime) - Дата и время окончания
totalSeats (int) - Общее количество мест на событии
availableSeats (int) - Текущее количество свободных мест

Booking

id (Guid) - Уникальный идентификатор брони
eventId (Guid) - Идентификатор события
status (BookingStatus) - Текущий статус брони
createdAt (DateTime) - Дата и время создания
processedAt (DateTime?) - Дата и время обработки

BookingStatus (enum)

Pending - Бронь создана, ожидает обработки
Confirmed - Бронь подтверждена
Rejected - Бронь отклонена

Параметры запроса (GET /api/events)

title (string) - Поиск по названию (регистронезависимый, частичное совпадение)
from (DateTime) - События, начинающиеся не раньше даты
to (DateTime) - События, заканчивающиеся не позже даты
page (int) - Номер страницы (по умолчанию 1)
pageSize (int) - Количество элементов на странице (по умолчанию 10)

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

Приложение содержит фоновый сервис BookingBackgroundService, который автоматически обрабатывает бронирования:
- Интервал опроса: 5 секунд
- Проверка: ищет брони со статусом Pending
- Обработка: параллельная (Task.WhenAll), для каждой брони выполняется задержка 2 секунды (имитация внешней системы)
- Результат: бронь переводится в статус Confirmed
- Заполнение: поле processedAt получает текущую дату и время
- При ошибке или удалении события: бронь переводится в статус Rejected, место возвращается в пул

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

Запрос: (тело пустое)
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

200 - Успешно
201 - Создано
202 - Принято в обработку
204 - Удалено
400 - Ошибка валидации
404 - Ресурс не найден
409 - Конфликт (нет свободных мест)
500 - Внутренняя ошибка сервера

Валидация

Title - Обязательное
StartAt - Обязательное
EndAt - Обязательное, должно быть позже StartAt
TotalSeats - Обязательное, должно быть больше 0

Технологии

- ASP.NET Core 10.0 (Preview)
- Entity Framework Core
- PostgreSQL
- Testcontainers
- Swagger / OpenAPI
- xUnit (тесты)
- C# 13.0