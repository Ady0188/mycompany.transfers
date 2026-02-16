# Унифицированный HTTP-клиент провайдеров (HttpProviderSender)

## Возможность и границы

**Да.** Все HTTP-провайдеры можно вести через один клиент **HttpProviderSender**: отправка запросов, заголовки, тело (JSON/XML), разбор ответа и маппинг статусов задаются конфигурацией в `SettingsJson` провайдера (операции в `Operations`). Oracle и провайдеры с нестандартной подписью (например, Sber с вызовом Java JAR) остаются отдельными клиентами.

## Что умеет HttpProviderSender

- **Операции** из `Operations` в настройках: для каждой операции задаются метод, путь, тело, заголовки, формат ответа.
- **Аутентификация**: Bearer (токен из настроек), Basic (user/password). HMAC по конфигу пока не реализован (заглушка).
- **Заголовки**: общие и по операции через `HeaderTemplate` (значения — шаблоны с подстановкой `[Account]`, `[TransferId]` и т.д.).
- **Тело запроса**: шаблон с подстановками и функциями из `TemplateFunctions` (в т.ч. `IPSEncryptData`, `GetTBankSignature` и др.).
- **Ответ**:
  - JSON: пути с точечной нотацией (`data.fullname`, `TransferState.State`) и опционально вывод в другое имя поля (`data.fullname|reciver_fullname`).
  - XML: XPath для полей ответа.
- **Успех/ошибка**: `SuccessField` + `SuccessValue`, при неуспехе — сообщение из `ErrorField`.
- **Маппинг статуса провайдера в OutboxStatus**: задаётся `ResponseStatusPath` (путь к полю со статусом) и `StatusMapping` (значение → имя `OutboxStatus`, ключ `"*"` — по умолчанию). Пример: `"CHECKED"` → `"SENDING"`, `"CONFIRMED"` → `"SUCCESS"`, `"*"` → `"FAILED"`.

## Добавление нового HTTP-провайдера (без своего клиента)

1. В БД не регистрировать отдельный `IProviderClient` с этим `ProviderId` — тогда `ProviderService` будет вызывать `HttpProviderSender` (общий отправитель).
2. В `SettingsJson` провайдера заполнить:
   - `Operations` — одна или несколько операций (`check`, `prepare`, `confirm`, `state` и т.д.).
   - Для каждой операции: `Method`, `PathTemplate`, при необходимости `BodyTemplate`, `Format` (json/xml), `ResponseFormat`, `HeaderTemplate` (если нужны свои заголовки).
   - Для разбора ответа: `ResponseField` (список путей через запятую; для JSON — точечная нотация, можно `path|outputKey`), `SuccessField`, `SuccessValue`, `ErrorField`.
   - Если у провайдера свой код статуса (CHECKED, CONFIRMED и т.п.): `ResponseStatusPath` и `StatusMapping` (см. пример ниже).

Пример фрагмента для TBank-подобного API (check):

```json
{
  "Operations": {
    "check": {
      "Method": "POST",
      "PathTemplate": "/v1/transfers/check",
      "BodyTemplate": "{ \"account\": \"[Account]\", ... }",
      "Format": "json",
      "ResponseFormat": "json",
      "HeaderTemplate": { "serviceName": "tbank" },
      "ResponseField": "TransferState.State|state,PlatformReferenceNumber|platform_reference_number,SettlementAmount.Amount|settlement_amount,SettlementAmount.Currency|settlement_currency",
      "ResponseStatusPath": "TransferState.State",
      "StatusMapping": {
        "CHECKED": "SENDING",
        "CHECK_PENDING": "TO_SEND",
        "*": "FAILED"
      },
      "SuccessField": null,
      "ErrorField": "TransferState.ErrorMessage"
    }
  }
}
```

Для ответа в виде вложенного JSON (как в Check с `data.fullname`) достаточно указать в `ResponseField` пути с точкой, например: `data.fullname,data.currencies` или `data.fullname|reciver_fullname,data.currencies|data.currencies`.

## Кто остаётся отдельным клиентом

- **Oracle (IBT)** — вызов хранимой процедуры, не HTTP.
- **Sber** — подпись XML через внешний Java JAR и клиентский сертификат; при необходимости можно вынести подпись в отдельный сервис и тогда описать Sber через общий HTTP-клиент.

Остальные HTTP-провайдеры (TBank, PayPorter, IPS, FIMI и любые новые), у которых запрос/ответ укладываются в шаблоны + JSON/XML-парсинг и маппинг статусов, можно переводить на один HttpProviderSender, задавая только конфигурацию.
