# MyCompany Transfers — описание сервиса и протоколов

## 1. Общее описание сервиса

**MyCompany Transfers** — сервис приёма и обработки переводов (денежных переводов). Он объединяет несколько направлений взаимодействия:

- **Админ-панель** — управление справочниками (агенты, провайдеры, услуги, терминалы, параметры, определения счетов, права доступа, курсы валют).
- **АБС (автоматизированная банковская система)** — зачисление/списание баланса агентов и добавление/обновление курсов валют (для проводок и лимитов).
- **Агенты (терминалы)** — приём переводов по протоколу MyCompany (Check → Prepare → Confirm, статус, баланс, курс).
- **Интеграция Tillabuy (НКО)** — приём запросов по протоколу Tillabuy (check, payment, getrate, getbalance) в формате XML.

Общая схема:

- Справочники и настройки задаются через **Admin API** (JWT).
- Балансы агентов и курсы валют приходят из **АБС** (API Key).
- Переводы идут от **терминалов агентов** (MyCompany — подпись по ApiKey + body) или от **Tillabuy** (по конфигу termid → agent, XML).

Ниже по отдельности описаны все протоколы (направления).

---

## 2. Протокол «Админ-панель» (Admin API)

**Назначение:** управление справочниками и настройками сервиса (CRUD агентов, провайдеров, услуг, терминалов, параметров, определений счетов, прав доступа агент–услуга/агент–валюта, просмотр курсов валют).

**Направление:** клиент админ-панели (или отдельный бэкенд с JWT) → сервис.

**Авторизация:** JWT Bearer. В заголовке `Authorization: Bearer <token>`. Токен должен содержать роли из конфига `Admin:AllowedRoles` (например, `TransfersAdmins`, `Domain Admins`). Параметры выпуска токена задаются в `Admin:Jwt` (Issuer, Audience, Key).

**Базовый путь:** `api/admin/...`

**Формат:** JSON (request/response). Ошибки — JSON (в т.ч. application/problem+json).

### Эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| GET   | `api/admin/agents` | Список агентов |
| GET   | `api/admin/agents/{id}` | Агент по Id |
| POST  | `api/admin/agents` | Создать агента |
| PUT   | `api/admin/agents/{id}` | Обновить агента |
| DELETE| `api/admin/agents/{id}` | Удалить агента |
| GET   | `api/admin/providers` | Список провайдеров |
| GET   | `api/admin/providers/{id}` | Провайдер по Id |
| POST  | `api/admin/providers` | Создать провайдера |
| PUT   | `api/admin/providers/{id}` | Обновить провайдера |
| DELETE| `api/admin/providers/{id}` | Удалить провайдера |
| GET   | `api/admin/services` | Список услуг |
| GET   | `api/admin/services/{id}` | Услуга по Id |
| POST  | `api/admin/services` | Создать услугу |
| PUT   | `api/admin/services/{id}` | Обновить услугу |
| DELETE| `api/admin/services/{id}` | Удалить услугу |
| GET   | `api/admin/terminals` | Список терминалов |
| GET   | `api/admin/terminals/{id}` | Терминал по Id |
| POST  | `api/admin/terminals` | Создать терминал |
| PUT   | `api/admin/terminals/{id}` | Обновить терминал |
| DELETE| `api/admin/terminals/{id}` | Удалить терминал |
| GET   | `api/admin/parameters` | Список параметров (ParamDefinition) |
| GET   | `api/admin/parameters/{id}` | Параметр по Id |
| POST  | `api/admin/parameters` | Создать параметр |
| PUT   | `api/admin/parameters/{id}` | Обновить параметр |
| DELETE| `api/admin/parameters/{id}` | Удалить параметр |
| GET   | `api/admin/account-definitions` | Список определений счетов |
| GET   | `api/admin/account-definitions/{id}` | Определение по Id (guid) |
| POST  | `api/admin/account-definitions` | Создать определение счёта |
| PUT   | `api/admin/account-definitions/{id}` | Обновить определение |
| DELETE| `api/admin/account-definitions/{id}` | Удалить определение |
| GET   | `api/admin/access/services` | Список доступов агент–услуга |
| GET   | `api/admin/access/services/{agentId}/{serviceId}` | Доступ по ключу |
| POST  | `api/admin/access/services` | Создать доступ агент–услуга |
| PUT   | `api/admin/access/services/{agentId}/{serviceId}` | Обновить доступ |
| DELETE| `api/admin/access/services/{agentId}/{serviceId}` | Удалить доступ |
| GET   | `api/admin/access/currencies` | Список доступов агент–валюта |
| GET   | `api/admin/access/currencies/{agentId}/{currency}` | Доступ по ключу |
| POST  | `api/admin/access/currencies` | Создать доступ агент–валюта |
| PUT   | `api/admin/access/currencies/{agentId}/{currency}` | Обновить доступ |
| DELETE| `api/admin/access/currencies/{agentId}/{currency}` | Удалить доступ |
| GET   | `api/admin/fx-rates` | Список курсов валют (опционально `?agentId=`) |
| GET   | `api/admin/fx-rates/{agentId}/{baseCurrency}/{quoteCurrency}` | Курс по ключу |

Добавление/обновление курсов валют (Upsert) выполняется по протоколу АБС (см. раздел 3), а не по Admin API.

---

## 3. Протокол «АБС» (Core Banking)

**Назначение:** операции со стороны автоматизированной банковской системы: зачисление/списание баланса агента (кредитование/дебитование) и добавление/обновление курсов валют. Используется при зачислении на счёт, открытии/закрытии овердрафта и при передаче курсов в сервис.

**Направление:** АБС → сервис.

**Авторизация:** API Key в заголовке `X-Abs-Api-Key`. Значение задаётся в конфиге `Abs:ApiKey`. Ни JWT админ-панели, ни подпись терминалов не используются.

**Базовые пути:** `api/abs/...` (кредит/дебит), `api/admin/fx-rates` только для метода POST (Upsert курса).

**Формат:** JSON.

### Эндпоинты АБС

| Метод | Путь | Описание |
|-------|------|----------|
| POST  | `api/abs/agents/{agentId}/credit` | Кредитование (зачисление) на баланс агента. Тело: `{ "currency": "USD", "amountMinor": 1000 }`. Ответ: `{ "currency": "USD", "balanceMinor": <новый баланс> }`. |
| POST  | `api/abs/agents/{agentId}/debit`  | Дебитование (списание) с баланса агента. Тело: `{ "currency": "USD", "amountMinor": 1000 }`. При недостатке средств — 409 Conflict. |
| POST  | `api/admin/fx-rates`              | Добавить или обновить курс валют (по ключу AgentId + BaseCurrency + QuoteCurrency). Тело — как у админского DTO курса (agentId, baseCurrency, quoteCurrency, rate, source). |

Кредит и дебит доступны только по `X-Abs-Api-Key`. Просмотр списка курсов и получение курса по ключу остаются в протоколе админ-панели (JWT).

---

## 4. Протокол «MyCompany Transfers» (агенты / терминалы)

**Назначение:** приём переводов от агентов (терминалов): проверка счёта, подготовка перевода (котировка), подтверждение, статус, баланс, курс валют.

**Направление:** терминал агента (или интеграция партнёра) → сервис.

**Авторизация:** по паре заголовков `X-Api-Key` (API Key терминала) и `X-MyCompany-Signature` (подпись тела запроса HMAC-SHA256 по секрету терминала). Терминал и агент определяются по ApiKey; подпись проверяется по телу запроса (JSON). Без валидной пары ключ+подпись запрос не принимается.

**Базовый путь:** `api/mycompany/transfers/v1`

**Формат:** JSON (request/response). Ошибки — JSON / application/problem+json.

### Эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| GET  | `api/mycompany/transfers/v1/check`   | Проверка счёта и параметров (serviceId, account, method). Возвращает доступные валюты и т.п. |
| POST | `api/mycompany/transfers/v1/prepare` | Подготовка перевода (внешний Id, счёт, сумма, валюты, услуга, параметры). Возвращает котировку. |
| POST | `api/mycompany/transfers/v1/confirm` | Подтверждение перевода по externalId и quotationId. |
| GET  | `api/mycompany/transfers/v1/status`  | Статус перевода по externalId или transferId. |
| GET  | `api/mycompany/transfers/v1/balance` | Баланс агента (опционально по currency). |
| GET  | `api/mycompany/transfers/v1/rates`   | Курс валют (query: baseCurrency, currency). |

Типовой сценарий: Check → Prepare → Confirm; статус и баланс/курс — по необходимости.

---

## 5. Протокол «Tillabuy» (НКО / XML)

**Назначение:** приём запросов по протоколу Tillabuy (НКО): проверка и оплата перевода, получение курса и баланса. Используется внешней системой (Tillabuy/НКО), которая обращается к сервису по единому entry point с параметром `function`.

**Направление:** внешняя система Tillabuy (НКО) → сервис.

**Авторизация:** по конфигу. Идентификация по query-параметру `termid`: в конфиге заданы `Tillabuy:Terminals` (termid → API Key терминала) и `Tillabuy:Agents` (API Key → agentId). Подпись тела (X-MyCompany-Signature) не используется; привязка termid → agent идёт через конфиг.

**Базовый путь:** `api` (один endpoint с query `function=...`).

**Формат:** запрос — query-параметры (и при необходимости тело); ответ — XML (application/xml). Коды ошибок по протоколу НКО (см. TillabuyExtensions.Errors в коде).

### Функции (направления запроса)

| function   | Описание |
|-----------|----------|
| `check`   | Проверка/подготовка перевода (аналог Check + Prepare). Параметры запроса маппятся в транзакцию (termid, счёт, сумма, валюта выдачи, услуга, параметры и т.д.). Возвращается ответ в формате Tillabuy (котировка/ошибка). |
| `payment` | Оплата/подтверждение перевода. По параметрам находится ранее подготовленный перевод; при совпадении суммы, счёта, услуги и имени отправителя выполняется Confirm или возвращается статус (OK/Error по кодам НКО). |
| `getrate` | Получение курса валют. Параметры: termid (обязателен), basecurrency, curisocode. Ответ — курс в формате Tillabuy. |
| `getbalance` | Получение баланса агента. Параметры: termid (обязателен), curisocode. Ответ — баланс в формате Tillabuy. |

Запрос приходит как GET на `api?function=check&...` (или иной function). Ошибки возвращаются в XML с полями Result, ErrCode, Description (и при необходимости другими по протоколу).

---

## 6. Сводная таблица направлений

| Направление        | Кто вызывает        | Авторизация              | Формат   | Основное назначение |
|--------------------|---------------------|---------------------------|----------|----------------------|
| Admin API          | Админ-панель        | JWT Bearer (роли)         | JSON     | Справочники, просмотр курсов |
| АБС                | АБС                 | X-Abs-Api-Key             | JSON     | Кредит/дебит агента, Upsert курсов |
| MyCompany Transfers| Терминалы агентов   | X-Api-Key + X-MyCompany-Signature | JSON | Переводы (check/prepare/confirm), статус, баланс, курс |
| Tillabuy           | НКО / Tillabuy      | Конфиг (termid → agent)   | XML (ответ) | check, payment, getrate, getbalance |

---

Документ описывает все протоколы и одно общее описание сервиса. При добавлении новых эндпоинтов или схем авторизации их нужно отразить в этом файле и в коде (контроллеры, атрибуты авторизации).
