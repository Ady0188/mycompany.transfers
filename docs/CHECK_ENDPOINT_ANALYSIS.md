# Глубокий анализ эндпоинта Check (MyCompanyController)

## 1. Точка входа и контракт

### 1.1 Маршрут и метод

| Параметр | Значение |
|----------|----------|
| **URL** | `GET api/mycompany/transfers/v1/check` |
| **Источник** | `ApiEndpoints.V1.MyCompanyTransfers.CheckAccount` → `"{Base}/check"`, Base = `api/mycompany/transfers/v1` |
| **Метод HTTP** | GET |
| **Контроллер** | `MyCompanyController.CheckAccount` |

### 1.2 Входные параметры (query)

| Параметр | Тип | Обязательность | Описание |
|----------|-----|----------------|----------|
| `serviceId` | `string` | не проверяется | Идентификатор услуги (из контракта) |
| `account` | `string` | не проверяется | Счёт/телефон/карта получателя |
| `method` | `int` | не проверяется | Приводится к `TransferMethod`: 0 = ByPhone, 1 = ByPan |

**Важно:** Нет DTO запроса и нет FluentValidation для `CheckCommand`. Пустые или невалидные значения не отсекаются на входе.

### 1.3 Аутентификация и авторизация

- На контроллере висит **`[SignatureAuthorize]`** (атрибут в `SignatureMiddleware.cs`).
- Запрос без заголовков `X-Api-Key` и `X-MyCompany-Signature` → **401** с `ApiErrorResponse`.
- Для GET тело пустое → подпись считается от пустой строки: `bodyHash = SHA256("")`, клиент должен подписывать то же самое.
- После проверки подписи:
  - по `X-Api-Key` из БД ищется терминал (`ITerminalRepository.GetByApiKeyAsync`);
  - в `User` проставляются claims: `agent_id`, `terminal_id`.
- **Риск:** `User.FindFirstValue("agent_id")!` — при отсутствии claim возможен NRE (например, если атрибут обойдут или зарегистрируют схему без установки claims). Рекомендуется явная проверка и 401.

---

## 2. Цепочка вызовов до провайдера

### 2.1 Контроллер → MediatR

```text
CheckAccount(serviceId, account, method, cancellationToken)
  → CheckCommand(agentId, serviceId, (TransferMethod)method, account)
  → _mediator.Send(query, cancellationToken)
  → result.Match(Ok, Problem)
```

- `agentId` берётся из `User` (claim после SignatureAuthorize).
- `method` кастуется в enum без проверки: значение вне 0/1 даёт неопределённый `TransferMethod` (например, 2 или -1). В хендлере **method нигде не используется** — по сути мёртвый параметр для Check.

### 2.2 CheckCommandHandler — порядок проверок

1. **Агент**  
   `_agents.GetForUpdateAsync(m.AgentId, ct)`  
   - Реализация: `AgentRepository` → EF `Agents.AsTracking().FirstOrDefaultAsync`.  
   - Нет блокировки `FOR UPDATE` (в отличие от `GetForUpdateSqlAsync`).  
   - Если `null` → **404** `AppErrors.Agents.NotFound`.

2. **Доступ агента к услуге**  
   `_access.GetAgentServiceAccessAsync(m.AgentId, m.ServiceId, ct)`  
   - Реализация: `AccessRepository` → таблица `AgentServiceAccess` (DbSet: `AgentServices`), фильтр по `AgentId`, `ServiceId` и `Enabled`.  
   - Если `access is null || !access.Enabled` → **403** Forbidden.

3. **Услуга**  
   `_services.GetByIdAsync(m.ServiceId, ct)`  
   - Реализация: `ServiceRepository` → `Services.Include(Parameters).FirstOrDefaultAsync`.  
   - Если `null` → **404** «Услуга не найдена».

4. **Балансы агента**  
   `agent.Balances` не null и не пустой.  
   - Иначе → **400** «Для агента не найдено ни одной валюты баланса».

5. **Валюта баланса и доступ по валюте**  
   Берётся первая валюта: `currency = agent.Balances.First().Key`.  
   - Порядок ключей в словаре не гарантирован — фактически «любая первая» валюта.  
   Затем `_access.IsCurrencyAllowedAsync(m.AgentId, currency, ct)` (таблица `AgentCurrencies`).  
   - Если валюта недоступна → **400** Validation.

6. **Провайдер**  
   `_providerService.ExistsEnabledAsync(service.ProviderId, ct)`  
   - В `ProviderService`: сначала ищется зарегистрированный `IProviderClient` с `ProviderId == service.ProviderId`; если найден — возвращается `true` без проверки БД.  
   - Иначе — загрузка провайдера из `IProviderRepository` и проверка `IsEnabled`.  
   - Если провайдер «не найден или отключён» → **400** Validation.

7. **Запрос к провайдеру**  
   Формируется `ProviderRequest` и вызывается `_providerService.SendAsync(service.ProviderId, providerReq, ct)`.

---

## 3. Формирование запроса к провайдеру (ProviderRequest)

В хендлере создаётся один раз:

```csharp
new ProviderRequest(
    agent.Id,                    // Source
    "check",                     // Operation
    null,                        // TransferId
    0,                           // NumId
    null,                        // ExternalId
    service.Id,                  // ServiceId
    service.ProviderServicveId,  // опечатка в имени свойства по всему проекту
    m.Account,                   // Account — единственное место использования account из запроса
    0,                           // CreditAmount
    0,                           // ProviderFee
    service.AllowedCurrencies.First(), // CurrencyIsoCode — первая разрешённая валюта услуги
    service.Name,                // Proc
    null,                        // Parameters
    null,                        // ProvReceivedParams
    DateTime.Now                 // TransferDateTime
);
```

- **Method (ByPhone/ByPan)** в `ProviderRequest` не передаётся — провайдер его не получает.
- **ProviderServicveId** — опечатка в названии (должно быть ProviderServiceId), повторяется в домене, контракте, БД и миграциях.

---

## 4. Доставка запроса до провайдера (ProviderService.SendAsync)

- Загружается провайдер: `_repo.GetAsync(providerId, ct)`.
- Ищется клиент: `_clients.FirstOrDefault(c => c.ProviderId == providerId)`.
- **Если клиент найден** (например, IBT, PayPorter, IPS, Sber, TBank, FIMI) → вызывается `client.SendAsync(provider, request, ct)`.
- **Если не найден** → вызывается `_sender.SendAsync(provider, request, ct)` — `HttpProviderSender`.

Зарегистрированные клиенты (из DI):  
`OracleProviderClient` (IBT), `PayPorterClient`, `IPSClient`, `FIMIClient`, `SberClient`, `TBankClient`.

### 4.1 Вариант A: Свой клиент (например, OracleProviderClient для IBT)

- Из `provider.SettingsJson` читается конфиг операций; для операции `"check"` берётся шаблон тела и т.п.
- `BuildReplacements()` строит словарь подстановок из `ProviderRequest` (в т.ч. `Account`, `ServiceId`, `ProviderServicveId` и т.д.).
- Тело запроса собирается по шаблону и передаётся в Oracle (хранимая процедура `company_mobile_banking.run_synch_query`).
- Ответ парсится по настройкам (SuccessField, ResponseField, ErrorField и т.д.).

**Критический баг в OracleProviderClient (строки 89–92):**

```csharp
response = response.Replace(...).Trim()...;
response = @"{""result"":0,...}";   // первая подстава
response = @"{""result"":0,""description"":""OK"",""data"":{""fullname"":""...",""currencies"":[""TJS"",""RUB"",""USD""]}}"; // вторая подстава — реальный ответ Oracle полностью перезаписывается
if (string.IsNullOrWhiteSpace(response))
    return null;  // возврат null при Task<ProviderResult> некорректен
```

Реальный ответ Oracle никогда не используется; для IBT Check всегда возвращаются одни и те же тестовые данные. Плюс `return null` несовместим с типом возврата.

### 4.2 Вариант B: HttpProviderSender (провайдер без своего клиента)

- Из настроек провайдера (`p.SettingsJson`) берётся конфиг операций; для `req.Operation` ("check") — метод, путь, тело, формат ответа.
- `BuildReplacements()` — тот же словарь подстановок (Account, ServiceId и т.д.).
- Собирается HTTP-запрос: `BaseAddress = p.BaseUrl`, путь и (при необходимости) тело из шаблона.
- Применяется авторизация (Bearer/Basic/Hamac из настроек).
- Выполняется запрос; ответ разбирается в `ParseResponseAsync` (JSON или XML по конфигу). Результат — `ProviderResult(OutboxStatus.SUCCESS/FAILED, ResponseFieldValues, ErrorMessage)`.

Поле ответа провайдера задаётся в настройках (например, XPath/JSON path). Хендлер ожидает ключи `data.fullname` и `data.currencies` в `ResponseFields`.

---

## 5. Обработка ответа провайдера в хендлере

- Успех только при `providerResult.Status == OutboxStatus.SETTING` или `OutboxStatus.SUCCESS`. Иначе → **400** «Provider check failed: {Error}».
- Из `providerResult.ResponseFields`:
  - `data.fullname` → `parameters["reciver_fullname"]` (опечатка: receiver).
  - `data.currencies` → строка, разбивается по запятой в список валют клиента.
- Если статус не SETTING и `clientCurrencies` пустой → **404** «Клиент не найден».
- Если статус SETTING → в качестве валют клиента подставляются `service.AllowedCurrencies` (обход для провайдеров в настройке).
- Для каждой валюты из пересечения «разрешённые услуге» и «вернул провайдер» запрашивается курс: `_fxRates.GetAsync(currency, curr, ct)` (базовая — первая валюта баланса агента). Нет курса — валюта пропускается.
- Если в итоге нет ни одной доступной валюты → **400** «Нет доступных валют для услуги».
- Ответ API: **200** и `CheckResponseDto`: `AvailableCurrencies` (список `CurrencyDto`: базовая валюта, валюта, курс) и `ResolvedParameters` (в т.ч. `reciver_fullname`).

---

## 6. Сводка потока данных (приём → пункт назначения)

| Этап | Где | Что происходит |
|------|-----|-----------------|
| 1 | HTTP GET | Запрос к `api/mycompany/transfers/v1/check?serviceId=...&account=...&method=...` |
| 2 | SignatureAuthorize | Проверка X-Api-Key, X-MyCompany-Signature (подпись тела; для GET тело ""). Установка agent_id, terminal_id в User. |
| 3 | MyCompanyController | Чтение agent_id из User, создание CheckCommand(agentId, serviceId, (TransferMethod)method, account). |
| 4 | CheckCommandHandler | Проверки: агент, доступ к услуге, услуга, балансы, валюта, провайдер. |
| 5 | ProviderRequest | Сборка запроса с Operation="check", Account из query, ServiceId, ProviderServicveId, первая AllowedCurrency и т.д. |
| 6 | ProviderService.SendAsync | Выбор: зарегистрированный IProviderClient (IBT, PayPorter, IPS, Sber, TBank, FIMI) или HttpProviderSender. |
| 7a | OracleProviderClient (IBT) | Oracle stored procedure; ответ перезаписывается захардкоженным JSON (баг). |
| 7b | Другие клиенты | Специфичная для провайдера логика (HTTP/другое API). |
| 7c | HttpProviderSender | HTTP по настройкам провайдера (BaseUrl, Operations["check"]), подстановки из ProviderRequest. |
| 8 | CheckCommandHandler | Парсинг ResponseFields (data.fullname, data.currencies), расчёт доступных валют по курсам, возврат CheckResponseDto. |
| 9 | MyCompanyController | result.Match(Ok, Problem) → 200 + JSON или Problem (4xx/5xx). |

---

## 7. Выявленные проблемы и рекомендации

### Критические

1. **OracleProviderClient** — ответ Oracle перезаписывается двумя захардкоженными строками (строки 89–90), плюс `return null` при пустом ответе при типе `Task<ProviderResult>`. Нужно убрать подставленный JSON и корректно обрабатывать пустой ответ (например, возвращать `ProviderResult` с ошибкой).
2. **Отсутствие валидации входа Check** — нет проверки на пустые/некорректные `serviceId`, `account`; `method` не проверяется на допустимые значения enum. Рекомендуется добавить `CheckCommandValidator` (FluentValidation).

### Важные

3. **Параметр method не используется** — в Check только передаётся в команду и нигде не читается. Либо использовать (например, передавать в провайдера или в логику), либо не требовать в контракте Check.
4. **Опечатка в ответе** — `reciver_fullname` → лучше `receiver_fullname` в ResolvedParameters.
5. **Опечатка в домене/БД** — `ProviderServicveId` → желательно поэтапно переименовать в `ProviderServiceId` (домен, контракт, миграции).
6. **agent_id / NRE** — явная проверка наличия `agent_id` после атрибута и возврат 401 при отсутствии.
7. **Валюта агента** — `agent.Balances.First().Key` не детерминирован. Имеет смысл явно выбрать валюту (например, по настройкам агента или по валюте услуги), а не «первую попавшуюся».

### Рекомендации

8. **Неиспользуемая зависимость** — в `CheckCommandHandler` внедрён `IParameterRepository _parameters`, но не используется. Удалить из конструктора.
9. **GetForUpdateAsync без блокировки** — для Check блокировка строки агента, возможно, не обязательна; тогда лучше переименовать в `GetByIdAsync` или завести отдельный метод без «ForUpdate», чтобы не вводить в заблуждение.
10. **Логирование** — убрать пустой `_logger.Warn("")` в ветке «Клиент не найден» или заменить на осмысленное сообщение.
11. **Документация контракта** — описать в контракте или в Swagger ожидаемый формат ответа провайдера (поля `data.fullname`, `data.currencies`) и форматы валют/разделителей.

---

## 8. Зависимости слоёв (Check)

- **Api:** SignatureAuthorize → TerminalRepository (по ApiKey), MediatR.
- **Application:** CheckCommand, CheckCommandHandler; репозитории: IAgentReadRepository, IAccessRepository, IServiceRepository, IFxRateRepository; IProviderService; хелперы AppErrors.
- **Domain:** Agent, AgentServiceAccess, Service, TransferMethod, CheckResponseDto, CurrencyDto, ProviderRequest (Application), OutboxStatus.
- **Infrastructure:** AgentRepository, CachedAgentReadRepository, AccessRepository, CachedAccessRepository, ServiceRepository, FxRateRepository, ProviderService, HttpProviderSender и/или IProviderClient (Oracle, PayPorter, IPS, Sber, TBank, FIMI), BuildReplacements/ApplyTemplate, парсинг ответов.

Документ можно использовать как основу для ревью остальных эндпоинтов (Prepare, Confirm, GetStatus, GetBalance, GetRate) и для пошагового исправления перечисленных замечаний.
