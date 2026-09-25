Events API

API для управления событиями и бронированиями с ролевой моделью и JWT-аутентификацией.

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
│   │   ├── EventsApi.Application/          Слой приложения
│   │   ├── EventsApi.Infrastructure/       Инфраструктурный слой
│   │   └── EventsApi.Presentation/         Слой представления (точка входа, Web API)
│   ├── events_api.Tests/                   Юнит-тесты
│   └── events_api.IntegrationTests/        Интеграционные тесты

Назначение слоёв

EventsApi.Domain — доменные сущности (Event, Booking, User), перечисления (BookingStatus, UserRole), доменные исключения (NotFoundException, ValidationException, NoAvailableSeatsException, EventAlreadyStartedException, BookingLimitExceededException, ForbiddenOperationException, BookingAlreadyCancelledException). Не зависит ни от каких внешних библиотек и фреймворков.

EventsApi.Application — бизнес-логика (use cases), интерфейсы сервисов (IEventService, IBookingService, IUserService), интерфейсы портов (IEventRepository, IBookingRepository, IUserRepository, IPasswordHasher, ITokenService), DTO. Зависит только от Domain.

EventsApi.Infrastructure — реализации портов: репозитории, AppDbContext, конфигурации маппинга, миграции, компоненты безопасности (PasswordHasher, JwtTokenService). Зависит от Application и Domain.

EventsApi.Presentation — контроллеры, middleware для обработки исключений, фоновая служба, Program.cs (composition root), настройка Swagger, JWT-аутентификация. Зависит от Application и Infrastructure.

Схема зависимостей

Presentation → Application + Infrastructure
Infrastructure → Application + Domain
Application → Domain
Domain → (ни от чего не зависит)

Ролевая модель и права

В системе две роли: User и Admin.

Публичные эндпоинты (без токена)

POST /api/auth/register — регистрация нового пользователя
POST /api/auth/login — вход в систему
GET /api/events — список событий с фильтрацией и пагинацией
GET /api/events/{id} — событие по ID

Только для аутентифицированных (роль User или Admin)

POST /api/events/{id}/book — создать бронь для события
GET /api/bookings/{id} — получить бронь по ID
DELETE /api/bookings/{id} — отменить бронь (владелец или Admin)

Только для Admin

POST /api/events — создать событие
PUT /api/events/{id} — обновить событие
DELETE /api/events/{id} — удалить событие

Правила доступа

- Пользователь может отменить только свою бронь.
- Администратор может отменить любую бронь.
- При попытке отменить чужую бронь без прав возвращается 403.
- Максимум активных броней у одного пользователя — 10 (при превышении 409).
- Нельзя забронировать событие, которое уже началось (возвращается 400).
- Повторная отмена уже отменённой брони возвращает 409.

Настройка базы данных

1. Установите PostgreSQL.
2. Создайте базу данных eventapi.
3. В файле src/EventsApi.Presentation/appsettings.json укажите строку подключения:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=ваш_пароль"
  }
}

Настройка JWT

Параметры JWT хранятся в appsettings.json в секции Jwt:

{
  "Jwt": {
    "Secret": "super-secret-key-minimum-32-characters-long-for-hmac-sha256-algorithm",
    "Issuer": "EventsApi",
    "Audience": "EventsApiClients",
    "ExpiresInMinutes": 60
  }
}

Поля

Secret — секретный ключ для подписи токена. Обязательно не короче 32 символов (требование алгоритма HMAC-SHA256).
Issuer — издатель токена.
Audience — аудитория, для которой предназначен токен.
ExpiresInMinutes — время жизни токена в минутах.

ВАЖНО ДЛЯ PRODUCTION

Не храните секрет JWT в appsettings.json в реальных проектах. Используйте один из безопасных способов:

- Переменные окружения (Jwt__Secret=...).
- Секреты .NET (dotnet user-secrets).
- Azure Key Vault, HashiCorp Vault, AWS Secrets Manager и т.п.

Также обязательно используйте уникальное случайное значение длиной не менее 32 символов. Пример генерации:

openssl rand -base64 48

Никогда не коммитьте реальный секрет в репозиторий.

Миграции

Схема базы данных управляется через миграции Entity Framework Core. Все миграции находятся в проекте EventsApi.Infrastructure.

При запуске приложения миграции применяются автоматически через db.Database.Migrate() в Program.cs.

Запуск приложения

1. Восстановите зависимости:
dotnet restore events_api/events_api.sln

2. Запустите приложение (точка входа — EventsApi.Presentation):
dotnet run --project events_api/src/EventsApi.Presentation

3. Откройте Swagger UI:
http://localhost:5286/swagger (профиль http)
https://localhost:7058/swagger (профиль https)

Как получить JWT-токен через Swagger

Шаг 1. Регистрация пользователя

1. Откройте Swagger UI.
2. Найдите POST /api/auth/register.
3. Нажмите Try it out.
4. Введите тело запроса:

{
  "login": "admin",
  "password": "admin123",
  "role": 1
}

Поле role: 0 — User, 1 — Admin. По умолчанию User.

5. Нажмите Execute.
6. В ответе вы получите объект с полем token — это JWT.

Шаг 2. Вход (если пользователь уже существует)

1. Найдите POST /api/auth/login.
2. Нажмите Try it out.
3. Введите:

{
  "login": "admin",
  "password": "admin123"
}

4. Нажмите Execute. В ответе будет поле token.

Шаг 3. Использование токена в Swagger

1. Нажмите кнопку Authorize (вверху справа).
2. В поле Value вставьте токен БЕЗ префикса Bearer (только сам токен).
3. Нажмите Authorize → Close.
4. Теперь все защищённые эндпоинты будут автоматически отправлять заголовок Authorization: Bearer <token>.

Шаг 4. Проверка

Вызовите POST /api/events (нужна роль Admin) или POST /api/events/{id}/book (нужна аутентификация). Если токен валиден и роль подходит, запрос выполнится.

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

Аутентификация

POST /api/auth/register — регистрация
POST /api/auth/login — вход, возвращает JWT

События

GET /api/events — список событий с фильтрацией и пагинацией (публичный)
GET /api/events/{id} — событие по ID (публичный)
POST /api/events — создать событие (только Admin)
PUT /api/events/{id} — обновить событие (только Admin)
DELETE /api/events/{id} — удалить событие (только Admin)

Бронирования

POST /api/events/{id}/book — создать бронь (требует аутентификации)
GET /api/bookings/{id} — получить бронь (требует аутентификации)
DELETE /api/bookings/{id} — отменить бронь (владелец или Admin)

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
userId (Guid) — идентификатор владельца брони
status (BookingStatus) — текущий статус брони
createdAt (DateTime) — дата и время создания
processedAt (DateTime?) — дата и время обработки

User

id (Guid) — уникальный идентификатор пользователя
login (string) — логин (уникальный)
passwordHash (string) — SHA-256 хеш пароля
role (UserRole) — роль (User или Admin)

BookingStatus (enum)

Pending — бронь создана, ожидает обработки
Confirmed — бронь подтверждена
Rejected — бронь отклонена
Cancelled — бронь отменена пользователем или администратором

UserRole (enum)

User — обычный пользователь
Admin — администратор

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

Регистрация

POST /api/auth/register

{
    "login": "admin",
    "password": "admin123",
    "role": 1
}

Ответ (200 OK):
{
    "userId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "login": "admin",
    "role": 1,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}

Логин

POST /api/auth/login

{
    "login": "admin",
    "password": "admin123"
}

Ответ (200 OK):
{
    "userId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "login": "admin",
    "role": 1,
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}

Создание события (только Admin)

POST /api/events
Authorization: Bearer <token>

{
    "title": "Новая конференция",
    "description": "Описание конференции",
    "startAt": "2025-07-01T10:00:00",
    "endAt": "2025-07-01T18:00:00",
    "totalSeats": 50
}

Бронирование

POST /api/events/{id}/book
Authorization: Bearer <token>

Запрос: тело пустое.

Ответ (202 Accepted):
{
    "id": "3f2c1d8e-1234-5678-9abc-def012345678",
    "eventId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "userId": "3f2c1d8e-1234-5678-9abc-def012345678",
    "status": 0,
    "createdAt": "2025-07-15T10:30:00.123Z",
    "processedAt": null
}

Ответ (400 Bad Request) — событие уже началось:
{
    "status": 400,
    "detail": "Нельзя забронировать событие, которое уже началось"
}

Ответ (409 Conflict) — нет мест или превышен лимит:
{
    "status": 409,
    "detail": "No available seats for this event"
}

Отмена брони

DELETE /api/bookings/{id}
Authorization: Bearer <token>

Ответ: 204 No Content

Ответ (403 Forbidden) — нет прав:
{
    "status": 403,
    "detail": "Нет прав на отмену этой брони"
}

Ответ (409 Conflict) — бронь уже отменена:
{
    "status": 409,
    "detail": "Бронь с id ... уже отменена"
}

Коды ответов

200 — успешно
201 — создано
202 — принято в обработку
204 — удалено
400 — ошибка валидации или событие уже началось
401 — не аутентифицирован
403 — нет прав на операцию
404 — ресурс не найден
409 — конфликт (нет мест, лимит броней, повторная отмена)
500 — внутренняя ошибка сервера

Валидация

Title — обязательное, до 200 символов
StartAt — обязательное
EndAt — обязательное, должно быть позже StartAt
TotalSeats — обязательное, должно быть больше 0
Login — обязательное, от 3 до 100 символов
Password — обязательное, минимум 6 символов

Технологии

- ASP.NET Core 10.0 (Preview)
- Entity Framework Core
- PostgreSQL
- JWT (System.IdentityModel.Tokens.Jwt)
- Testcontainers
- Swagger / OpenAPI
- xUnit (тесты)
- C# 13.0