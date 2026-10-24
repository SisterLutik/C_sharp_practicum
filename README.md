Events API

Микросервисная система управления событиями и бронированиями с асинхронной обработкой через Kafka и кэшированием в Redis.

Требования

- .NET 10.0 (Preview) или выше
- Docker Desktop (для запуска всей системы)
- PostgreSQL (для локальной разработки без Docker)
- Kafka (для локальной разработки без Docker)
- Redis (для локальной разработки без Docker)

Архитектура

Система состоит из трёх независимых микросервисов и общего проекта контрактов. Каждый сервис построен по принципам чистой архитектуры (Domain, Application, Infrastructure, Presentation) и имеет свою базу данных.

Схема зависимостей между сервисами

UserService      →  выдаёт JWT-токен
EventService     →  управляет событиями и местами, кэширует события в Redis
BookingService   →  управляет бронями, публикует BookingConfirmed в Kafka
Kafka            →  BookingService публикует, EventService читает
Redis            →  EventService использует для кэширования

Обмен между сервисами

- Kafka: BookingService публикует BookingConfirmed при подтверждении брони. EventService читает сообщение и уменьшает AvailableSeats у события.
- JWT: токен выдаёт только UserService, остальные сервисы проверяют его по общему секрету.
- Прямого HTTP-взаимодействия между сервисами нет.
- Redis: используется только внутри EventService, другие сервисы его не знают.

Структура решения

events_api/                              корень репозитория
├── events_api.sln                       файл решения
├── src/
│   ├── EventsApi.Contracts/            общий проект контрактов
│   │   └── Kafka/                      BookingConfirmed, KafkaTopics, KafkaJsonOptions
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
│   │   │   └── Settings/               CacheSettings (ConnectionString, TTL)
│   │   ├── EventService.Infrastructure/
│   │   │   └── Caching/                RedisSettings удалён, RedisCacheService
│   │   └── EventService.Presentation/
│   │
│   └── BookingService/
│       ├── BookingService.Domain/
│       ├── BookingService.Application/
│       ├── BookingService.Infrastructure/
│       └── BookingService.Presentation/
│
├── tests/
│   ├── UserService.Tests/
│   ├── EventService.Tests/
│   ├── BookingService.Tests/
│   └── IntegrationTests/
│
└── events_api/                          Docker-инфраструктура
    ├── docker-compose.yml
    ├── Dockerfile
    ├── .env.example
    ├── .env                             (не в git)
    └── init-db/
        └── 01-create-databases.sql

Назначение слоёв

- Domain — доменные сущности (User, Event, Booking), перечисления (UserRole, BookingStatus), доменные исключения. Не зависит ни от чего внешнего.

- Application — бизнес-логика, интерфейсы сервисов и портов (IUserRepository, IEventPublisher, IBookingRepository, ICacheService), DTO, настройки (CacheSettings). Зависит только от Domain.

- Infrastructure — реализации портов: репозитории, DbContext, миграции, Kafka-продюсеры и консьюмеры, RedisCacheService, компоненты безопасности. Зависит от Application и Domain.

- Presentation — контроллеры, middleware, Program.cs, фоновые сервисы. Зависит от Application и Infrastructure.

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
- GET /api/events/top — публичный
- GET /api/events/{id} — публичный
- POST /api/events — только Admin
- PUT /api/events/{id} — только Admin
- DELETE /api/events/{id} — только Admin

BookingService:
- GET /api/bookings/{id} — требуется аутентификация
- POST /api/bookings/events/{eventId} — требуется аутентификация
- DELETE /api/bookings/{id} — требуется аутентификация, владелец или Admin

Доменные правила

- Максимум 10 активных броней у одного пользователя (возвращается 409)
- Нельзя отменить чужую бронь без прав Admin (возвращается 403)
- Нельзя отменить уже отменённую бронь (возвращается 409)

Проверка «событие существует / не началось / есть места» выполняется в EventService при получении BookingConfirmed из Kafka. Если проверка не проходит — места не уменьшаются, а в лог пишется предупреждение. Пользователь узнаёт об этом, когда бронь не подтвердилась.

Запуск через Docker

Это основной способ запуска — одной командой поднимается вся система.

Docker-файлы находятся в папке events_api/events_api/.

1. Перейдите в папку с Docker-файлами:

cd events_api

2. Скопируйте шаблон секретов:

cp .env.example .env

3. Отредактируйте .env, установив уникальный JWT_SECRET (не короче 32 символов).

4. Запустите:

docker compose up --build

5. Проверьте, что все контейнеры поднялись:

docker compose ps

Ожидается шесть контейнеров: events-kafka, events-postgres, events-redis, events-user-service, events-event-service, events-booking-service.

6. Откройте Swagger UI:

- http://localhost:5001/swagger — UserService
- http://localhost:5002/swagger — EventService
- http://localhost:5003/swagger — BookingService

Схема портов

Docker внутренний порт — 8080 для всех сервисов. На хост маппятся:

Порт на хосте | Сервис
5001 | UserService
5002 | EventService
5003 | BookingService
5432 | PostgreSQL (один инстанс, три базы)
6379 | Redis (для отладки через redis-cli с хоста)
9093 | Kafka (внешний listener для подключения с хоста)

Остановка:

docker compose down

Остановка с удалением данных:

docker compose down -v

Запуск без Docker

1. Установите PostgreSQL, Kafka и Redis.

2. Создайте три базы:

psql -U postgres -c "CREATE DATABASE users_db;"
psql -U postgres -c "CREATE DATABASE events_db;"
psql -U postgres -c "CREATE DATABASE bookings_db;"

3. Убедитесь, что Redis запущен на localhost:6379. Быстрый способ через Docker:

docker run -d --name events-redis -p 6379:6379 redis:7-alpine

4. В каждом Presentation настройте секреты через user-secrets:

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

5. Запустите каждый сервис в отдельном терминале из корня репозитория:

dotnet run --project src/UserService/UserService.Presentation
dotnet run --project src/EventService/EventService.Presentation
dotnet run --project src/BookingService/BookingService.Presentation

Убедитесь, что в launchSettings.json у каждого сервиса свой порт (5001, 5002, 5003).

Настройка JWT

Параметры JWT хранятся в appsettings.json в секции Jwt:

- Issuer — EventsApi
- Audience — EventsApiClients
- ExpiresInMinutes — 60

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

Настройка Redis

Redis используется только в EventService для кэширования чтения событий.

Параметры кэша хранятся в appsettings.json EventService в секции Cache:

- ConnectionString — строка подключения к Redis (localhost:6379 локально, redis:6379 в Docker)
- InstanceName — префикс ключей (events:)
- EventTtlMinutes — TTL одного события в минутах (5)
- TopEventsTtlMinutes — TTL топа событий в минутах (10)

В Docker строка подключения переопределяется переменной Cache__ConnectionString=redis:6379 в docker-compose.yml. Внутри Docker-сети сервисы обращаются друг к другу по имени контейнера, поэтому redis, а не localhost.

Проверка подключения:

docker exec events-redis redis-cli ping

Ожидается PONG.

Кэширование

В EventService используется паттерн Cache-Aside для двух сценариев чтения.

Что кэшируется:

Ключ | Что | TTL
event:{id} | Одно событие по id | 5 минут
events:top10 | Топ-10 событий по проценту проданных мест | 10 минут

Процент продаж считается по формуле (totalSeats - availableSeats) / totalSeats. Отсортированный по убыванию список из первых десяти событий возвращается эндпоинтом GET /api/events/top.

Почему такие TTL:

- event:{id} — 5 минут. Отдельное событие меняется редко, а при изменении ключ инвалидируется явно. Даже если инвалидация не сработает, устаревание ограничено пятью минутами.
- events:top10 — 10 минут. Это рейтинговый агрегат, который меняется медленно. Явная инвалидация при каждом бронировании была бы избыточной, список всё равно пересобирается нечасто.

Стратегия инвалидации:

Для отдельного события используется инвалидация при записи. При обновлении или удалении события ключ event:{id} удаляется из кэша. Следующий читающий запрос обратится к БД и прогреет кэш заново. Такой подход выбран вместо обновления при записи, потому что он проще и не требует маппинга доменной сущности в DTO внутри метода сохранения.

Топ-10 при мутациях не инвалидируется. Кэш обновляется только по TTL.

Порядок операций:

Сначала изменение сохраняется в базу данных, и только потом инвалидируется кэш. Если выполнение оборвётся между этими двумя шагами, в БД останутся актуальные данные, а кэш при следующем запросе просто обновится. Обратный порядок (сначала кэш, потом БД) создал бы риск потерять актуальные данные.

Инвалидация в фоновой обработке:

BookingConfirmedConsumer при получении сообщения из Kafka уменьшает AvailableSeats у события и после SaveChangesAsync инвалидирует ключ event:{EventId}. Иначе кэш вернул бы старое количество мест.

Поведение при недоступности Redis:

Ошибки Redis логируются, но не пробрасываются. Если Redis упал:

- GetAsync возвращает null, запрос идёт в БД
- SetAsync тихо пропускает запись (данные уже в БД)
- RemoveAsync тихо пропускает удаление (ключ истечёт по TTL)

Клиент не получает ошибку, запрос обслуживается напрямую из БД. Для быстрого переподключения у ConnectionMultiplexer установлен AbortOnConnectFail=false: после восстановления Redis кэш начнёт работать автоматически без перезапуска сервиса.

Базы данных

Каждый сервис имеет свою базу:

Сервис | База
UserService | users_db (таблица Users)
EventService | events_db (таблицы Events, ProcessedBookings)
BookingService | bookings_db (таблица Bookings)

Базы физически живут в одном инстансе PostgreSQL, но логически изолированы. Между таблицами разных сервисов нет внешних ключей — связь только по идентификаторам (UserId, EventId).

Миграции

Схема управляется через EF Core миграции. Миграции применяются автоматически при старте каждого сервиса.

Создание новой миграции для конкретного сервиса (из корня репозитория):

dotnet ef migrations add ИмяМиграции --project src/UserService/UserService.Infrastructure --startup-project src/UserService/UserService.Presentation

Аналогично для EventService и BookingService.

Фоновая обработка броней

BookingService содержит фоновый сервис BookingBackgroundService, который каждые 5 секунд ищет брони в статусе Pending и обрабатывает их через BookingProcessor.

Логика обработки:

1. Задержка 2 секунды (имитация обращения к внешней системе)
2. Бронь переводится в статус Confirmed и сохраняется в bookings_db
3. В Kafka публикуется BookingConfirmed

EventService читает BookingConfirmed и уменьшает AvailableSeats у соответствующего события в events_db.

Асинхронная обработка через Kafka

Это ключевая часть системы. Поток данных:

1. Пользователь создаёт бронь: POST /api/bookings/events/{eventId}
   → BookingService сохраняет Booking со статусом Pending.
   Места в EventService пока НЕ уменьшаются.

2. Фоновая служба BookingBackgroundService каждые 5 секунд ищет брони в статусе Pending и передаёт их в BookingProcessor.

3. BookingProcessor после задержки (2 секунды, имитация внешней системы) вызывает BookingManager.ConfirmBookingAsync:
   - меняет статус на Confirmed и сохраняет в bookings_db;
   - публикует BookingConfirmed в Kafka.

4. EventService.BookingConfirmedConsumer читает сообщение и:
   - проверяет, не обрабатывалась ли эта бронь раньше (идемпотентность);
   - проверяет, что событие существует и ещё не началось;
   - уменьшает AvailableSeats на 1 и сохраняет в events_db;
   - инвалидирует кэш event:{EventId};
   - записывает отметку в ProcessedBookings.

5. Если Kafka недоступна, сообщение останется в топике и будет доставлено при перезапуске.

Преимущества такой схемы:

- EventService не зависит от доступности BookingService в момент уменьшения мест
- Kafka гарантирует at-least-once доставку
- При сбое одного сервиса второй продолжает работать

Идемпотентность

Kafka гарантирует доставку at-least-once — одно сообщение может прийти дважды (например, если EventService упал между SaveChangesAsync и Commit).

Чтобы не уменьшить места дважды, EventService хранит обработанные BookingId в таблице ProcessedBookings. Первичный ключ — BookingId, поэтому повторная вставка невозможна. При повторной доставке consumer:

1. Проверяет, есть ли BookingId в ProcessedBookings
2. Если есть — пропускает сообщение с записью в лог

Формат сообщения BookingConfirmed

Поле | Тип | Описание
bookingId | Guid | Идентификатор брони
eventId | Guid | Идентификатор события
userId | Guid | Идентификатор пользователя
seatsCount | int | Количество мест (обычно 1)
confirmedAt | DateTime | Момент подтверждения (UTC)

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

- Логин: admin
- Пароль: admin123

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

Тесты кэширования в EventService.Tests покрывают:

- при попадании в кэш репозиторий не вызывается;
- при промахе данные берутся из репозитория и сохраняются в кэш с TTL из CacheSettings;
- при мутирующих операциях кэш инвалидируется после сохранения в БД (проверяется порядок операций).

Основные эндпоинты

UserService (порт 5001)

- POST /api/auth/register — регистрация
- POST /api/auth/login — вход, возвращает JWT

EventService (порт 5002)

- GET /api/events — список с фильтрацией и пагинацией
- GET /api/events/top — топ-10 событий по проценту проданных мест (кэшируется)
- GET /api/events/{id} — событие по ID (кэшируется)
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

Код | Описание
200 | Успешно
201 | Создано
202 | Принято в обработку
204 | Удалено
400 | Ошибка валидации или событие уже началось
401 | Не аутентифицирован
403 | Нет прав на операцию
404 | Ресурс не найден
409 | Конфликт (нет мест, лимит броней, повторная отмена)
500 | Внутренняя ошибка

Безопасность

- Пароли хешируются через PBKDF2 с солью и 100 000 итераций
- JWT подписывается через HMAC-SHA256
- Секреты не хранятся в репозитории
- Admin создаётся только через сид в Program.cs UserService, а не через API регистрации

Технологии

- ASP.NET Core 10.0
- Entity Framework Core 10
- PostgreSQL 16
- Redis 7 (StackExchange.Redis)
- Apache Kafka (KRaft)
- Confluent.Kafka
- JWT (System.IdentityModel.Tokens.Jwt)
- Testcontainers
- Swagger / OpenAPI
- xUnit, FluentAssertions, Moq
- Docker, Docker Compose
- C# 13.0

Полезные команды

Сборка решения (из корня репозитория):

dotnet build

Очистка:

dotnet clean

Переход в папку Docker-файлов:

cd events_api

Просмотр логов конкретного сервиса:

docker compose logs -f event-service

Перезапуск одного сервиса:

docker compose restart booking-service

Подключение к PostgreSQL:

docker exec -it events-postgres psql -U postgres -d events_db

Проверка сообщений в Kafka:

docker exec events-kafka /opt/kafka/bin/kafka-console-consumer.sh --bootstrap-server localhost:9092 --topic booking-confirmed --from-beginning

Проверка Redis:

docker exec events-redis redis-cli ping
docker exec events-redis redis-cli KEYS "events:*"
docker exec events-redis redis-cli TTL "events:top10"

Удаление всех данных (включая тома):

docker compose down -v