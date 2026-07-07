# Events API

API для управления событиями.

## Требования

- .NET 6.0 или выше

## Запуск

bash
dotnet restore
dotnet run


Swagger: `https://localhost:5001/swagger`

## Эндпоинты

| Метод | Эндпоинт | Описание |
|-------|----------|----------|
| GET | `/api/events` | Получить все события |
| GET | `/api/events/{id}` | Получить событие по ID |
| POST | `/api/events` | Создать событие |
| PUT | `/api/events/{id}` | Полностью обновить событие |
| DELETE | `/api/events/{id}` | Удалить событие |
|-------|----------|----------|----------------|
| POST | `/api/events/{id}/book` | Создать бронь для события | 202 Accepted |
| GET | `/api/bookings/{id}` | Получить бронь по ID | 200 OK |

## Примеры

### GET /api/events

json
[
  {
    "id": 1,
    "title": "Интенсив по ловле жуков",
    "description": "Увлекательный аттракцион",
    "startAt": "2025-06-12T10:00:00",
    "endAt": "2025-06-14T18:00:00"
  }
]


### GET /api/events/1

json
{
  "id": 1,
  "title": "Интенсив по ловле жуков",
  "description": "Увлекательный аттракцион",
  "startAt": "2025-06-12T10:00:00",
  "endAt": "2025-06-14T18:00:00"
}


### POST /api/events

json
{
  "title": "Новая конференция",
  "description": "Описание конференции",
  "startAt": "2025-07-01T10:00:00",
  "endAt": "2025-07-01T18:00:00"
}


### PUT /api/events/1

json
{
  "id": 1,
  "title": "Обновлённое название",
  "description": "Новое описание",
  "startAt": "2025-08-01T10:00:00",
  "endAt": "2025-08-01T20:00:00"
}


### DELETE /api/events/1

Ответ: `204 No Content`


### POST /api/events/1/book

**Запрос:** (тело пустое)

**Ответ (202 Accepted):**
json
{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "eventId": 1,
    "status": 0,
    "createdAt": "2025-07-15T10:30:00.123Z",
    "processedAt": null
}
Заголовок Location:

text
Location: /api/bookings/3f2c1d8e-1234-5678-9abc-def012345678
GET /api/bookings/3f2c1d8e-1234-5678-9abc-def012345678
Ответ (200 OK):

json
{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "eventId": 1,
    "status": 1,
    "createdAt": "2025-07-15T10:30:00.123Z",
    "processedAt": "2025-07-15T10:30:15.456Z"
}


## Модель Event

| Поле | Тип | Обязательное | Описание |
|------|-----|--------------|----------|
| `id` | int | Да | Уникальный идентификатор события |
| `title` | string | Да | Название события |
| `description` | string | Нет | Описание события |
| `startAt` | DateTime | Да | Дата и время начала события |
| `endAt` | DateTime | Да | Дата и время окончания события |

## Модель Booking

| Поле | Тип | Описание |
|------|-----|----------|
| `id` | Guid | Уникальный идентификатор брони |
| `eventId` | int | ID события |
| `status` | BookingStatus | Статус брони (Pending, Confirmed, Rejected) |
| `createdAt` | DateTime | Дата создания |
| `processedAt` | DateTime? | Дата обработки |

## Статусы бронирования

| Статус | Описание |
|--------|----------|
| `Pending` | Бронь создана, ожидает обработки |
| `Confirmed` | Бронь подтверждена |
| `Rejected` | Бронь отклонена |


## Валидация

| Поле | Правило |
|------|---------|
| Title | Обязательное |
| StartAt | Обязательное |
| EndAt | Обязательное, должно быть позже StartAt |

## Коды ответов

| Код | Описание |
|-----|----------|
| 200 | Успешно |
| 201 | Создано |
| 204 | Удалено (нет содержимого) |
| 400 | Ошибка валидации |
| 404 | Событие не найдено |

## Технологии

- ASP.NET Core 6.0
- Swagger
- C# 10.0
