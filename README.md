# IdentityService

Сервис аутентификации, авторизации и администрирования пользователей платформы Scoodle. Он выдаёт JWT/refresh-токены, публикует JWKS для других сервисов, управляет ролями и блокировками, а также хранит журнал административных действий.

## Возможности

- регистрация и вход пользователей;
- JWT access-токены с подписью RS256;
- ротация и отзыв refresh-токенов;
- роли `Admin`, `Teacher`, `Student`;
- административное создание и просмотр пользователей;
- атомарная замена набора ролей;
- блокировка и разблокировка с отзывом refresh-сессий;
- аудит входов и административных действий;
- OpenID Connect discovery и JWKS;
- Swagger/OpenAPI и типизированный .NET-клиент.

## Технологии

- .NET 10 / ASP.NET Core Minimal API;
- ASP.NET Core Identity;
- Entity Framework Core 10;
- PostgreSQL;
- FluentValidation;
- Swashbuckle/OpenAPI;
- xUnit, Shouldly и Testcontainers.

## Структура решения

| Проект | Назначение |
| --- | --- |
| `Host` | Точка входа, конфигурация и запуск приложения |
| `Web` | Minimal API, CQRS-срезы, авторизация, токены и Swagger |
| `Domain` | Пользователи, refresh-токены и события аудита |
| `Data` | `DbContext`, EF-конфигурации, миграции и сидинг |
| `Contracts` | HTTP-контракты, DTO, роли и маршруты |
| `Client` | Типизированный HTTP-клиент IdentityService |
| `Common` | Общие CQRS и result-примитивы |
| `Tests/IntegrationTests` | Интеграционные тесты с PostgreSQL Testcontainers |

Функциональность в `Web` организована вертикальными срезами. Endpoint, команда или запрос, валидатор и handler одной операции находятся рядом.

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/);
- PostgreSQL 16 или совместимая версия;
- Docker Desktop для интеграционных тестов;
- Rider, Visual Studio или другая IDE с поддержкой .NET.

## Быстрый запуск

### 1. Запустить PostgreSQL

Локальная конфигурация ожидает базу `identity_db` на порту `5432`:

```powershell
docker run --name identity-postgres `
  -e POSTGRES_DB=identity_db `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:16-alpine
```

При повторном запуске существующего контейнера:

```powershell
docker start identity-postgres
```

### 2. Запустить сервис

Из корня репозитория:

```powershell
dotnet run --project Host/Host.csproj
```

Профиль Rider `IdentityService.Host` использует:

- окружение `Development`;
- адрес `http://localhost:5101`;
- стартовую страницу `http://localhost:5101/swagger`.

При старте сервис автоматически применяет EF Core migrations и выполняет идемпотентный сидинг ролей и настроенных пользователей.

### 3. Проверить сервис

| Ресурс | URL |
| --- | --- |
| Swagger UI | `http://localhost:5101/swagger` |
| OpenAPI JSON | `http://localhost:5101/swagger/v1/swagger.json` |
| Health | `http://localhost:5101/health` |
| JWKS | `http://localhost:5101/.well-known/jwks.json` |
| OpenID discovery | `http://localhost:5101/.well-known/openid-configuration` |

Если Swagger UI сообщает `Failed to load API definition`, сначала откройте URL OpenAPI JSON напрямую и проверьте консоль приложения. После перезапуска сервиса может потребоваться обновление страницы через `Ctrl+F5`.

## Тестовые пользователи

В окружении `Development` создаются следующие учётные записи:

| Роль | Email | Пароль |
| --- | --- | --- |
| Admin | `admin@scoodle.local` | `Admin1234` |
| Teacher | `teacher@scoodle.local` | `Teacher1234` |
| Student | `student@scoodle.local` | `Student1234` |

Эти данные предназначены только для локальной разработки. Для production секцию `InitialUsers` необходимо переопределить или оставить пустой.

## Авторизация в Swagger

1. Выполните `POST /api/v1/auth/login` под нужным пользователем.
2. Скопируйте `accessToken` из ответа.
3. Нажмите **Authorize** в верхней части Swagger UI.
4. Вставьте только значение JWT, без префикса `Bearer`.

Swagger самостоятельно добавит заголовок:

```http
Authorization: Bearer <accessToken>
```

## Основные endpoint-ы

### Аутентификация

| Метод | Путь | Назначение |
| --- | --- | --- |
| `POST` | `/api/v1/auth/register` | Регистрация пользователя с ролью Student |
| `POST` | `/api/v1/auth/login` | Получение access/refresh-токенов |
| `POST` | `/api/v1/auth/refresh` | Ротация пары токенов |
| `POST` | `/api/v1/auth/logout` | Отзыв refresh-токена |

### Администрирование пользователей

Все операции требуют роль `Admin`.

| Метод | Путь | Назначение |
| --- | --- | --- |
| `GET` | `/api/v1/users` | Список пользователей с фильтрами и пагинацией |
| `POST` | `/api/v1/users` | Создание пользователя без входа от его имени |
| `GET` | `/api/v1/users/{id}` | Карточка пользователя |
| `PUT` | `/api/v1/users/{id}/roles` | Атомарная замена полного набора ролей |
| `POST` | `/api/v1/users/{id}/block` | Блокировка и отзыв refresh-токенов |
| `POST` | `/api/v1/users/{id}/unblock` | Снятие блокировки |
| `GET` | `/api/v1/users/{id}/activity` | Журнал активности пользователя |
| `GET` | `/api/v1/audit-events` | Общий журнал аудита |

Сервис запрещает снять у себя роль `Admin`, заблокировать собственную учётную запись и удалить доступ у последнего активного администратора.

## Токены и интеграция сервисов

Access-токен:

- подписывается алгоритмом RS256;
- содержит `sub`, email, имя, `jti` и роли;
- по умолчанию действует 15 минут;
- проверяется другими сервисами через issuer, audience и JWKS.

Refresh-токен:

- по умолчанию действует 7 дней;
- хранится в БД только в виде SHA-256 хэша;
- ротируется при обновлении;
- отзывается при logout, повторном использовании или блокировке пользователя.

Блокировка не отзывает уже выданный access JWT: он остаётся валидным до своего срока истечения. Это сохраняет автономную проверку JWT в остальных сервисах. Login и refresh заблокированного пользователя отклоняются.

Для локальной разработки используются:

```json
{
  "Jwt": {
    "Issuer": "http://localhost:5101",
    "Audience": "scoodle-api",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

## Конфигурация

Основные секции находятся в `Host/appsettings.json` и `Host/appsettings.Development.json`.

| Параметр | Назначение |
| --- | --- |
| `ConnectionStrings:ConnectionString` | Подключение к PostgreSQL |
| `Jwt:Issuer` | Издатель JWT и адрес discovery/JWKS |
| `Jwt:Audience` | Ожидаемая аудитория токена |
| `Jwt:AccessTokenMinutes` | Срок действия access-токена |
| `Jwt:RefreshTokenDays` | Срок действия refresh-токена |
| `Cors:AllowedOrigins` | Разрешённые origin фронтенда |
| `SigningKey:PrivateKeyPem` | RSA private key в PEM |
| `SigningKey:KeyFilePath` | Путь к файлу RSA private key |
| `SigningKey:Kid` | Необязательный явный идентификатор ключа |
| `InitialUsers` | Пользователи, создаваемые при старте |

Для переменных окружения вложенные ключи записываются через двойное подчёркивание:

```powershell
$env:ConnectionStrings__ConnectionString = "Host=localhost;Port=5432;Database=identity_db;Username=postgres;Password=postgres"
$env:Jwt__Issuer = "http://localhost:5101"
$env:Jwt__Audience = "scoodle-api"
```

Если RSA-ключ не задан, сервис создаёт временный RSA-2048 ключ при старте. Для стабильности токенов между перезапусками и для production необходимо передать `SigningKey:PrivateKeyPem` или постоянный `SigningKey:KeyFilePath`. Закрытый ключ нельзя коммитить в репозиторий.

## CORS

По умолчанию Development-конфигурация разрешает фронтенд:

```text
http://localhost:5173
```

Другие адреса добавляются в массив `Cors:AllowedOrigins`. После изменения конфигурации сервис необходимо перезапустить.

## Миграции

Миграции находятся в `Data/Migrations` и автоматически применяются при старте приложения.

Для создания новой миграции при установленном `dotnet-ef`:

```powershell
dotnet ef migrations add MigrationName --project Data/Data.csproj --startup-project Host/Host.csproj
```

Миграции не следует удалять или переписывать после их применения в общих окружениях.

## Тесты

Интеграционные тесты всегда используют отдельный PostgreSQL Testcontainer. Строка подключения из обычного `appsettings` в тестовый host не передаётся.

Docker Desktop должен быть запущен, после чего из корня выполняется:

```powershell
dotnet format IdentityService.sln --no-restore
dotnet build IdentityService.sln --no-restore
dotnet test IdentityService.sln --no-build --no-restore
```

Testcontainers создаёт базу `identity_test`, применяет миграции и удаляет контейнер после завершения набора тестов.

## Аудит

Сервис записывает:

- успешный вход;
- административное создание пользователя;
- изменение ролей;
- блокировку;
- разблокировку.

Событие содержит actor, target, тип, безопасное описание и время. Пароли, access-токены и refresh-токены в аудит не записываются. Журнал поддерживает фильтры по actor, target, типу и диапазону дат.

## Production

Перед развёртыванием необходимо:

- убрать тестовых пользователей и пароли;
- передавать строку подключения через secret storage;
- настроить постоянный RSA-ключ;
- установить корректные HTTPS issuer и audience;
- ограничить `Cors:AllowedOrigins` реальными адресами фронтенда;
- не публиковать Swagger без отдельного решения по доступу;
- настроить резервное копирование PostgreSQL и ротацию ключей.
