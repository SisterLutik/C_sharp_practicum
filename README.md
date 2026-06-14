# C\_sharp\_practicum

homework for yandex.practicum



\# Events API



API для управления событиями. Поддерживает полный CRUD и частичное обновление событий.



\## 1. 📋 Требования



\- .NET 6.0 или выше

\- Любая операционная система (Windows, Linux, macOS)



\## 🚀 Запуск проекта



\### 1. Клонирование репозитория



```bash

git clone <url-репозитория>

cd events\\\\\\\_api

\###  2. Восстановление зависимостей
bash
dotnet restore
\### 3. Установка Swagger (если ещё не установлен)
bash
dotnet add package Swashbuckle.AspNetCore
\### 4. Запуск приложения
bash
dotnet run
\###  5. Открытие Swagger UI
После запуска откройте в браузере:

text
https://localhost:5001/swagger
Или, если используете HTTP:

text
http://localhost:5000/swagger
💡 Совет: Swagger автоматически откроется при запуске, если в launchSettings.json указано "launchUrl": "swagger"

\### 📚 Документация API
Базовый URL: https://localhost:5001/api/events

Эндпоинты
Метод	Эндпоинт	Описание	Успешный ответ
GET	/api/events	Получить список всех событий	200 OK
GET	/api/events/{id}	Получить событие по ID	200 OK
POST	/api/events	Создать новое событие	201 Created
PATCH	/api/events/{id}	Частичное обновление события	200 OK
DELETE	/api/events/{id}	Удалить событие	204 No Content
1. GET /api/events — получить все события
Ответ:

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
2. GET /api/events/{id} — получить событие по ID
Пример запроса: GET /api/events/1

Ответ (200 OK):

json
{
  "id": 1,
  "title": "Интенсив по ловле жуков",
  "description": "Увлекательный аттракцион",
  "startAt": "2025-06-12T10:00:00",
  "endAt": "2025-06-14T18:00:00"
}
Ошибка (404 Not Found):

json
{
  "message": "Событие с id 999 не найдено"
}
3. POST /api/events — создать событие
Тело запроса:

json
{
  "title": "Новая конференция",
  "description": "Описание конференции",
  "startAt": "2025-07-01T10:00:00",
  "endAt": "2025-07-01T18:00:00"
}
Ответ (201 Created):

json
{
  "id": 3,
  "title": "Новая конференция",
  "description": "Описание конференции",
  "startAt": "2025-07-01T10:00:00",
  "endAt": "2025-07-01T18:00:00"
}
Ошибка валидации (400 Bad Request):

json
{
  "message": "EndAt должен быть позже StartAt"
}
4. PATCH /api/events/{id} — частичное обновление
Обновляет только переданные поля. Остальные остаются без изменений.

Пример запроса: PATCH /api/events/1

Тело запроса:

json
{
  "title": "Новое название события",
  "endAt": "2025-12-31T18:00:00"
}
Ответ (200 OK):

json
{
  "message": "Событие успешно обновлено",
  "updatedFields": ["Title", "EndAt"],
  "event": {
    "id": 1,
    "title": "Новое название события",
    "description": "Старое описание",
    "startAt": "2025-06-12T10:00:00",
    "endAt": "2025-12-31T18:00:00"
  }
}
5. DELETE /api/events/{id} — удалить событие
Пример запроса: DELETE /api/events/1

Ответ: 204 No Content (пустое тело)

Ошибка (404 Not Found):

json
{
  "message": "Событие с id 999 не найдено"
}
✅ Валидация
При создании и обновлении событий применяются следующие правила:

Поле	Правило
Title	Обязательное поле
StartAt	Обязательное поле
EndAt	Обязательное поле, должно быть позже StartAt
🛠️ Технологии
ASP.NET Core 6.0 — веб-фреймворк

Swagger / OpenAPI — документация API

C# 10.0 — язык программирования

📁 Структура проекта
text
events_api/
├── Controllers/
│   └── EventsController.cs      # REST API контроллер
├── Models/
│   ├── Event.cs                  # Модель события
│   └── UpdateEventRequest.cs     # DTO для обновления
├── Interfaces/
│   └── IEventService.cs          # Интерфейс сервиса
├── Data/
│   └── EventService.cs           # Реализация сервиса
├── Program.cs                    # Настройка приложения
└── README.md                     # Документация
🧪 Тестирование через Swagger
Запустите приложение: dotnet run

Откройте Swagger UI: https://localhost:5001/swagger

Разверните нужный эндпоинт

Нажмите Try it out

Заполните параметры

Нажмите Execute

Swagger покажет запрос и ответ сервера.

📝 Примечания
Данные хранятся в памяти (List). При перезапуске приложения все изменения сбрасываются.

Для постоянного хранения потребуется подключение базы данных (Entity Framework Core).


