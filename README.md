Events API

Микросервисная система управления событиями и бронированиями с асинхронной обработкой через Kafka.

Требования

- .NET 10.0 (Preview) или выше
- Docker Desktop (для запуска всей системы)
- PostgreSQL (для локальной разработки без Docker)
- Kafka (для локальной разработки без Docker)

Архитектура

Система состоит из трёх независимых микросервисов и общего проекта контрактов. Каждый сервис построен по принципам чистой архитектуры (Domain, Application, Infrastructure, Presentation) и имеет свою базу данных.

Схема зависимостей между сервисами:

  UserService  ←  выдаёт JWT-токен
  
  EventService  ←  управляет событиями и местами
  
  BookingService  →  EventService (HTTP, для резервирования мест)
  
  BookingService  →  EventService (Kafka, booking-confirmed)
  
  EventService  →  BookingService (HTTP, для проверки событий)

Обмен между сервисами:

- HTTP: BookingService вызывает EventService для проверки события и резервирования места.
- Kafka: BookingService публикует событие BookingConfirmed, EventService его читает и уменьшает доступные места.
- JWT: токен выдаёт только UserService, остальные сервисы его проверяют.

Структура решения

events_api/
├── events_api.sln
├── docker-compose.yml
├── Dockerfile
├── .env.example
├── .env                         (не в git)
├── init-db/
│   └── 01-create-databases.sql
├── src/
│   ├── EventsApi.Contracts/    общий проект контрактов
│   │   ├── Events/
│   │   ├── Users/
│   │   └── Kafka/              BookingConfirmed, KafkaTopics, KafkaJsonOptions
│   │
│   ├── UserService/
│   │   ├── UserService.Domain/
│   │   ├── UserService.Application/
│   │   ├── UserService.Infrastructure/
│   │   └── UserService.Presentation/
│   │
│   ├── EventService/
│   │   ├── EventService.Domain/
│   │   ├── EventService.Application/
│   │   ├── EventService.Infrastructure/
│   │   └── EventService.Presentation/
│   │
│   └── BookingService/
│       ├── BookingService.Domain/
│       ├── BookingService.Application/
│       ├── BookingService.Infrastructure/
│       └── BookingService.Presentation/
└── tests/
    ├── UserService.Tests/
    ├── EventService.Tests/
    ├── BookingService.Tests/
    └── IntegrationTests/

Назначение слоёв

Domain — доменные сущности (User, Event, Booking), перечисления (UserRole, BookingStatus), доменные исключения. Не зависит ни от чего внешнего.

Application — бизнес-логика, интерфейсы сервисов и портов (IUserRepository, IEventServiceClient, IEventPublisher), DTO. Зависит только от Domain.

Infrastructure — реализации портов: репозитории, DbContext, миграции, HTTP-клиенты, Kafka-продюсеры и консьюмеры, компоненты безопасности. Зависит от Application и Domain.

Presentation — контроллеры, middleware, Program.cs, фоновые сервисы. Зависит от Application и Infrastructure.

Роли и доступ

UserRole имеет два значения:

- User — обычный пользователь. Может просматривать события, создавать и отменять свои брони.
- Admin — администратор. Может создавать, обновлять и удалять события, отменять любые брони.

Права по эндпоинтам

UserService:

- POST /api/auth/register — доступен без токена
- POST /api/auth/login — доступен без токена

EventService:

- GET /api/events — публичный
- GET /api/events/{id} — публичный
- POST /api/events — только Admin
- PUT /api/events/{id} — только Admin
- DELETE /api/events/{id} — только Admin
- POST /api/events/{id}/reserve — внутренний, вызывается BookingService
- POST /api/events/{id}/release — внутренний, вызывается BookingService

BookingService:

- GET /api/bookings/{id} — требуется аутентификация
- POST /api/bookings/events/{eventId} — требуется аутентификация
- DELETE /api/bookings/{id} — требуется аутентификация, владелец или Admin

Доменные правила

- Нельзя забронировать событие, которое уже началось (возвращается 400)
- Максимум 10 активных броней у одного пользователя (возвращается 409)
- Нельзя отменить чужую бронь без прав Admin (возвращается 403)
- Нельзя отменить уже отменённую бронь (возвращается 409)
- Нельзя забронировать при отсутствии мест (возвращается 409)

Запуск через Docker

Это основной способ запуска — одной командой поднимается вся система.

1. Скопируйте шаблон секретов:

cp .env.example .env

2. Отредактируйте .env, установив уникальный JWT_SECRET (не короче 32 символов).

3. Запустите:

docker compose up --build

4. Проверьте, что все контейнеры поднялись:

docker compose ps

5. Откройте Swagger UI:

http://localhost:5001/swagger — UserService
http://localhost:5002/swagger — EventService
http://localhost:5003/swagger — BookingService

Схема портов

Docker внутренний порт — 8080 для всех сервисов. На хост маппятся:

- 5001 → UserService
- 5002 → EventService
- 5003 → BookingService
- 5432 → PostgreSQL (один инстанс, три базы)
- 9093 → Kafka (внешний listener для подключения с хоста)

Остановка:

docker compose down

Остановка с удалением данных:

docker compose down -v

Запуск без Docker

Если хотите запустить сервисы локально:

1. Установите PostgreSQL и Kafka.

2. Создайте три базы:

psql -U postgres -c "CREATE DATABASE users_db;"
psql -U postgres -c "CREATE DATABASE events_db;"
psql -U postgres -c "CREATE DATABASE bookings_db;"

3. В каждом Presentation настройте секреты через user-secrets:

cd src/UserService/UserService.Presentation
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "super-secret-key-minimum-32-characters-long-for-hmac-sha256"

cd ../../EventService/EventService.Presentation
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "super-secret-key-minimum-32-characters-long-for-hmac-sha256"

cd ../../BookingService/BookingService.Presentation
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "super-secret-key-minimum-32-characters-long-for-hmac-sha256"

Секрет должен быть одинаковым во всех трёх сервисах.

4. Запустите каждый сервис в отдельном терминале:

dotnet run --project src/UserService/UserService.Presentation
dotnet run --project src/EventService/EventService.Presentation
dotnet run --project src/BookingService/BookingService.Presentation

Убедитесь, что в launchSettings.json у каждого сервиса свой порт (5001, 5002, 5003).

Настройка JWT

Параметры JWT хранятся в appsettings.json в секции Jwt:

  Issuer — EventsApi
  Audience — EventsApiClients
  ExpiresInMinutes — 60

Сам секрет (Secret) в appsettings.json НЕ хранится. Он передаётся:

- В Docker — через переменную окружения Jwt__Secret (в .env)
- Локально — через user-secrets

ВАЖНО: секрет, Issuer и Audience должны совпадать во всех трёх сервисах. Иначе токен, выданный UserService, не пройдёт проверку в EventService и BookingService.

Для генерации безопасного секрета:

openssl rand -base64 48

Настройка Kafka

Kafka запускается в режиме KRaft (без Zookeeper). Один узел работает и как broker, и как controller.

Топики:

- booking-confirmed — публикуется BookingService при подтверждении брони, читается EventService

Ключ сообщения — EventId. Это гарантирует, что все брони по одному событию попадут в один раздел и обработаются последовательно.

Топик создаётся автоматически при старте EventService через KafkaTopicInitializer. Если топик уже существует — ничего не происходит.

Ручная проверка топика:

docker exec events-kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic booking-confirmed --from-beginning

Базы данных

Каждый сервис имеет свою базу:

- users_db — таблица Users
- events_db — таблица Events
- bookings_db — таблица Bookings

Базы физически живут в одном инстансе PostgreSQL, но логически изолированы. Между таблицами разных сервисов нет внешних ключей — связь только по идентификаторам (UserId, EventId).

Миграции

Схема управляется через EF Core миграции. Миграции применяются автоматически при старте каждого сервиса.

Создание новой миграции для конкретного сервиса:

dotnet ef migrations add ИмяМиграции --project src/UserService/UserService.Infrastructure --startup-project src/UserService/UserService.Presentation

Аналогично для EventService и BookingService.

Фоновая обработка броней

BookingService содержит фоновый сервис BookingBackgroundService, который каждые 5 секунд ищет брони в статусе Pending и обрабатывает их через BookingProcessor.

Логика обработки:

1. Задержка 2 секунды (имитация обращения к внешней системе)
2. Проверка, что событие существует через EventService (HTTP)
3. Если событие не найдено — бронь переводится в Rejected
4. Если событие найдено — бронь переводится в Confirmed
5. Событие сохраняется в БД BookingService
6. В Kafka публикуется BookingConfirmed

EventService читает BookingConfirmed и уменьшает AvailableSeats у соответствующего события в events_db.

Асинхронная обработка через Kafka

Это ключевая часть системы. Поток данных:

1. Пользователь создаёт бронь: POST /api/bookings/events/{eventId}
2. BookingService резервирует место через HTTP POST к EventService /reserve
3. Через 5-7 секунд фоновая служба подтверждает бронь и публикует BookingConfirmed в Kafka
4. EventService получает сообщение и уменьшает AvailableSeats в своей БД
5. Если Kafka недоступна, сообщение останется в топике и будет доставлено при перезапуске

Преимущества такой схемы:

- EventService не зависит от доступности BookingService в момент уменьшения мест
- Kafka гарантирует at-least-once доставку
- При сбое одного сервиса второй продолжает работать

Формат сообщения BookingConfirmed

  bookingId — Guid брони
  eventId — Guid события
  userId — Guid пользователя
  seatsCount — количество мест (обычно 1)
  confirmedAt — момент подтверждения (UTC)

Пример:

{
  "bookingId": "11111111-1111-1111-1111-111111111111",
  "eventId": "22222222-2222-2222-2222-222222222222",
  "userId": "33333333-3333-3333-3333-333333333333",
  "seatsCount": 1,
  "confirmedAt": "2026-01-15T10:30:00Z"
}

Получение JWT-токена

Шаг 1. Логин администратора. При первом запуске UserService автоматически создаёт админа:

  Логин: admin
  Пароль: admin123

POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "login": "admin",
  "password": "admin123"
}

В ответе будет поле token.

Шаг 2. Использование токена в Swagger:

1. Откройте Swagger UI нужного сервиса
2. Нажмите Authorize
3. Вставьте токен БЕЗ префикса Bearer
4. Нажмите Authorize, потом Close
5. Все защищённые запросы будут автоматически отправлять токен

Шаг 3. Проверка через curl:

curl -X POST http://localhost:5002/api/events -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d "{\"title\":\"Конференция\",\"startAt\":\"2026-12-01T10:00:00Z\",\"endAt\":\"2026-12-01T18:00:00Z\",\"totalSeats\":50}"

Запуск тестов

Все тесты:

dotnet test

Юнит-тесты конкретного сервиса:

dotnet test tests/UserService.Tests/UserService.Tests.csproj
dotnet test tests/EventService.Tests/EventService.Tests.csproj
dotnet test tests/BookingService.Tests/BookingService.Tests.csproj

Интеграционные тесты (требуют запущенный Docker):

dotnet test tests/IntegrationTests/IntegrationTests.csproj

Юнит-тесты используют InMemory-провайдер EF Core, интеграционные — Testcontainers с реальным PostgreSQL в Docker.

Основные эндпоинты

UserService (порт 5001)

- POST /api/auth/register — регистрация
- POST /api/auth/login — вход, возвращает JWT

EventService (порт 5002)

- GET /api/events — список с фильтрацией и пагинацией
- GET /api/events/{id} — событие по ID
- POST /api/events — создать (Admin)
- PUT /api/events/{id} — обновить (Admin)
- DELETE /api/events/{id} — удалить (Admin)

BookingService (порт 5003)

- GET /api/bookings/{id} — бронь по ID
- POST /api/bookings/events/{eventId} — создать бронь
- DELETE /api/bookings/{id} — отменить бронь

Модели данных

User

- id (Guid)
- login (string, уникальный)
- passwordHash (string, PBKDF2)
- role (UserRole)

Event

- id (Guid)
- title (string)
- description (string?)
- startAt (DateTime, UTC)
- endAt (DateTime, UTC)
- totalSeats (int)
- availableSeats (int)

Booking

- id (Guid)
- eventId (Guid)
- userId (Guid)
- status (BookingStatus)
- createdAt (DateTime, UTC)
- processedAt (DateTime?, UTC)

BookingStatus

- Pending — создана, ожидает обработки
- Confirmed — подтверждена
- Rejected — отклонена
- Cancelled — отменена

UserRole

- User — обычный пользователь
- Admin — администратор

Коды ответов

- 200 — успешно
- 201 — создано
- 202 — принято в обработку
- 204 — удалено
- 400 — ошибка валидации или событие уже началось
- 401 — не аутентифицирован
- 403 — нет прав на операцию
- 404 — ресурс не найден
- 409 — конфликт (нет мест, лимит броней, повторная отмена)
- 500 — внутренняя ошибка

Безопасность

- Пароли хешируются через PBKDF2 с солью и 100 000 итераций
- JWT подписывается через HMAC-SHA256
- Секреты не хранятся в репозитории
- Admin создаётся только через сид в Program.cs UserService, а не через API регистрации

Технологии

- ASP.NET Core 10.0
- Entity Framework Core 10
- PostgreSQL 16
- Apache Kafka (KRaft)
- Confluent.Kafka
- JWT (System.IdentityModel.Tokens.Jwt)
- Testcontainers
- Swagger / OpenAPI
- xUnit, FluentAssertions, Moq
- Docker, Docker Compose
- C# 13.0

Полезные команды

Сборка решения:

dotnet build

Очистка:

dotnet clean

Просмотр логов конкретного сервиса:

docker compose logs -f event-service

Перезапуск одного сервиса:

docker compose restart booking-service

Подключение к PostgreSQL:

docker exec -it events-postgres psql -U postgres -d events_db

Проверка сообщений в Kafka:

docker exec events-kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic booking-confirmed --from-beginning

Удаление всех данных (включая тома):

docker compose down -v