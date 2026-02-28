# ТЗ: Админ-панель и запросы АБС

Документ описывает техническое задание на админ-панель MyCompany Transfers и протокол запросов со стороны АБС (автоматизированной банковской системы).

---

## 1. Админ-панель

### 1.1 Назначение

Веб-приложение для администраторов сервиса переводов. Обеспечивает:

- **Вход** по учётным данным Active Directory (логин/пароль), получение JWT для доступа к Admin API.
- **Управление справочниками:** агенты, провайдеры, услуги, терминалы, параметры, определения счетов.
- **Управление доступами:** агент–услуга, агент–валюта.
- **Просмотр курсов валют** (добавление/обновление курсов выполняется АБС, не админ-панелью).

### 1.2 Технологии и доступ

| Параметр | Значение |
|----------|----------|
| Стек | Blazor (Interactive Server + WebAssembly), MudBlazor |
| Базовый URL API | Задаётся в конфиге админки `ApiBaseUrl` (по умолчанию `https://localhost:7001`) |
| Авторизация к API | JWT Bearer в заголовке `Authorization: Bearer <token>` |

Роли пользователя должны входить в список `Admin:AllowedRoles` в конфиге API (например, `TransfersAdmins`, `Domain Admins`). Параметры JWT задаются в `Admin:Jwt` (Issuer, Audience, Key).

### 1.3 Разделы интерфейса

| Раздел | Маршрут | Описание |
|--------|---------|----------|
| Главная | `/` | Приветствие и краткое описание |
| Курсы валют | `/fx-rates` | Список курсов (опционально фильтр по агенту) |
| Агенты | `/agents` | CRUD агентов |
| Провайдеры | `/providers` | CRUD провайдеров |
| Услуги | `/services` | CRUD услуг |
| Терминалы | `/terminals` | CRUD терминалов |
| Параметры | `/parameters` | CRUD параметров (ParamDefinition) |
| Определения счетов | `/account-definitions` | CRUD определений счетов |
| Доступы (услуги) | `/access-services` | CRUD доступов агент–услуга |
| Доступы (валюты) | `/access-currencies` | CRUD доступов агент–валюта |

Выход — кнопка в шапке; при отсутствии/истечении токена — редирект на `/login`.

### 1.4 Соответствие Admin API

Админ-панель обращается к эндпоинтам под базовым путём `api/admin/...`. Полный перечень методов — в [PROTOCOLS.md](PROTOCOLS.md), раздел «Протокол Админ-панель (Admin API)».

Кратко:

- **Агенты:** `GET/POST/PUT/DELETE api/admin/agents`, `GET api/admin/agents/{id}`
- **Провайдеры:** `GET/POST/PUT/DELETE api/admin/providers`, `GET api/admin/providers/{id}`
- **Услуги:** `GET/POST/PUT/DELETE api/admin/services`, `GET api/admin/services/{id}`
- **Терминалы:** `GET/POST/PUT/DELETE api/admin/terminals`, `GET api/admin/terminals/{id}`
- **Параметры:** `GET/POST/PUT/DELETE api/admin/parameters`, `GET api/admin/parameters/{id}`
- **Определения счетов:** `GET/POST/PUT/DELETE api/admin/account-definitions`, `GET api/admin/account-definitions/{id}`
- **Доступы услуги:** `GET/POST/PUT/DELETE api/admin/access/services`, `GET api/admin/access/services/{agentId}/{serviceId}`
- **Доступы валюты:** `GET/POST/PUT/DELETE api/admin/access/currencies`, `GET api/admin/access/currencies/{agentId}/{currency}`
- **Курсы валют:** `GET api/admin/fx-rates` (список, опционально `?agentId=`), `GET api/admin/fx-rates/{agentId}/{baseCurrency}/{quoteCurrency}` (по ключу)

Формат обмена — JSON. Ошибки — JSON (в т.ч. `application/problem+json`).

---

## 2. Запросы АБС

### 2.1 Назначение

Протокол для автоматизированной банковской системы (АБС):

- **Кредитование/дебитование** баланса агента (зачисление/списание при проводках, овердрафте и т.п.).
- **Добавление/обновление курсов валют** (Upsert) для использования в проводках и лимитах.

Направление вызова: **АБС → сервис MyCompany Transfers**.

### 2.2 Авторизация

| Параметр | Значение |
|----------|----------|
| Заголовок | `X-Abs-Api-Key` |
| Значение | Секрет из конфига API `Abs:ApiKey` |
| Примечание | JWT админ-панели и подпись терминалов не используются |

В конфиге (`appsettings.json`): секция `Abs`, ключ `ApiKey`. В проде необходимо задать стойкий ключ (не значение по умолчанию `CHANGE_ME_ABS_API_KEY_FOR_CREDIT_DEBIT_FX`).

### 2.3 Базовый URL и формат

- **Базовый путь для кредит/дебит:** `api/abs/...`
- **Добавление/обновление курса:** `api/admin/fx-rates` (только метод POST, с заголовком `X-Abs-Api-Key`)
- **Формат:** JSON (request/response). Ошибки — JSON / `application/problem+json`.

### 2.4 Эндпоинты АБС

#### 2.4.1 Кредитование (зачисление)

**Запрос**

| Метод | URL | Заголовки |
|-------|-----|------------|
| POST | `api/abs/agents/{agentId}/credit` | `Content-Type: application/json`, `X-Abs-Api-Key: <ключ>` |

**Тело запроса (JSON)**

```json
{
  "currency": "USD",
  "amountMinor": 1000,
  "docId": 12345
}
```

| Поле | Тип | Обязательное | Описание |
|------|-----|--------------|----------|
| currency | string | да | Код валюты (например, USD, RUB, TJS) |
| amountMinor | long | да | Сумма в минорных единицах (центы, копейки и т.д.) |
| docId | long | да | Идентификатор документа в АБС (для истории/связки) |

**Ответ при успехе (200 OK)**

```json
{
  "currency": "USD",
  "balanceMinor": 15000
}
```

`balanceMinor` — баланс агента по данной валюте после зачисления (в минорных единицах).

---

#### 2.4.2 Дебитование (списание)

**Запрос**

| Метод | URL | Заголовки |
|-------|-----|------------|
| POST | `api/abs/agents/{agentId}/debit` | `Content-Type: application/json`, `X-Abs-Api-Key: <ключ>` |

**Тело запроса (JSON)** — то же, что и для кредита:

```json
{
  "currency": "USD",
  "amountMinor": 1000,
  "docId": 12346
}
```

**Ответ при успехе (200 OK)**

```json
{
  "currency": "USD",
  "balanceMinor": 14000
}
```

**Ошибка при недостатке средств:** `409 Conflict` (тело — JSON/ProblemDetails).

---

#### 2.4.3 Добавление/обновление курса валют (Upsert)

**Запрос**

| Метод | URL | Заголовки |
|-------|-----|------------|
| POST | `api/admin/fx-rates` | `Content-Type: application/json`, `X-Abs-Api-Key: <ключ>` |

**Тело запроса (JSON)**

Ключ курса: пара агент + базовая и котируемая валюта. При совпадении ключа запись обновляется, иначе создаётся новая.

```json
{
  "agentId": "Agent1",
  "baseCurrency": "USD",
  "quoteCurrency": "RUB",
  "rate": 92.50,
  "source": "abs"
}
```

| Поле | Тип | Обязательное | Описание |
|------|-----|--------------|----------|
| agentId | string | да | Идентификатор агента |
| baseCurrency | string | да | Базовая валюта (например, USD) |
| quoteCurrency | string | да | Котируемая валюта (например, RUB) |
| rate | number | да | Значение курса (decimal) |
| source | string | нет | Источник курса (по умолчанию "manual", для АБС можно "abs") |

В ответе при успехе возвращается полный DTO курса (в т.ч. `id`, `updatedAtUtc`, `isActive`). При ошибке валидации (например, агент не найден, base = quote) — 400 с описанием.

### 2.5 Коды ответов и ошибки

| Код | Ситуация |
|-----|----------|
| 200 | Успех (credit, debit, Upsert курса) |
| 400 | Ошибка валидации (тело/параметры) |
| 401 | Отсутствует или неверный `X-Abs-Api-Key` |
| 404 | Агент не найден (для credit/debit/fx-rates) |
| 409 | Дебит: недостаточно средств на балансе |

Ошибки возвращаются в формате JSON (в т.ч. `application/problem+json`) с полями типа `type`, `title`, `status`, `detail`, при необходимости `errors`.

### 2.6 Сводная таблица запросов АБС

| Операция | Метод | URL | Тело |
|----------|--------|-----|------|
| Кредит | POST | `api/abs/agents/{agentId}/credit` | `{ "currency", "amountMinor", "docId" }` |
| Дебит | POST | `api/abs/agents/{agentId}/debit` | `{ "currency", "amountMinor", "docId" }` |
| Upsert курса | POST | `api/admin/fx-rates` | `{ "agentId", "baseCurrency", "quoteCurrency", "rate", "source"? }` |

Во всех запросах обязателен заголовок: `X-Abs-Api-Key: <значение из конфига Abs:ApiKey>`.

---

Документ согласован с реализацией в коде: `AbsController`, `FxRatesController` (POST с `[AbsApiKeyAuthorize]`), `PROTOCOLS.md`.
