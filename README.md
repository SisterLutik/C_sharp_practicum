Events API

API для управления событиями и бронированиями.

Требования
- .NET 10.0 (Preview) или выше
- Любая ОС (Windows, Linux, macOS)

Запуск проекта
1. Восстановите зависимости:
dotnet restore

2. Запустите приложение:
dotnet run

3. Откройте Swagger UI:
https://localhost:5286/swagger

Запуск тестов
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

Фоновая обработка броней

Приложение содержит фоновый сервис BookingBackgroundService, который автоматически обрабатывает бронирования:
- Интервал опроса: 5 секунд
- Проверка: ищет брони со статусом Pending
- Обработка: для каждой брони выполняется задержка 2 секунды (имитация внешней системы)
- Результат: бронь переводится в статус Confirmed
- Заполнение: поле processedAt получает текущую дату и время

Пример жизненного цикла брони:
1. POST /api/events/{id}/book -> бронь создаётся со статусом Pending
2. Фоновый сервис находит бронь через 5 секунд
3. Через 2 секунды обработки статус меняется на Confirmed
4. processedAt заполняется временем обработки

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
            "endAt": "2025-07-05T18:00:00"
        }
    ]
}

Примеры запросов

POST /api/events
{
    "title": "Новая конференция",
    "description": "Описание конференции",
    "startAt": "2025-07-01T10:00:00",
    "endAt": "2025-07-01T18:00:00"
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
    "endAt": "2025-08-01T20:00:00"
}

DELETE /api/events/{id}
Ответ: 204 No Content

Формат ответа при ошибках
{
    "status": 400,
    "detail": "EndAt должен быть позже StartAt"
}

Коды ответов
200 - Успешно
201 - Создано
202 - Принято в обработку
204 - Удалено
400 - Ошибка валидации
404 - Ресурс не найден
500 - Внутренняя ошибка сервера

Валидация
Title - Обязательное
StartAt - Обязательное
EndAt - Обязательное, должно быть позже StartAt

Технологии
- ASP.NET Core 10.0 (Preview)
- Swagger / OpenAPI
- xUnit (тесты)
- C# 13.0