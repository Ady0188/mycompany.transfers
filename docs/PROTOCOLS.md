# Мультипротокольная поддержка API

API поддерживает несколько протоколов обмена для разных агентов/партнёров. Каждый протокол реализуется отдельным контроллером (или группой контроллеров) со своими:

- **Базовым путём (route prefix)** — чтобы разделять трафик по протоколам.
- **Формат обмена** — JSON или XML (Consumes/Produces, при необходимости кастомные форматтеры).
- **Авторизацией** — у каждого протокола может быть своя схема (API-ключ + подпись, конфиг termid→agent, и т.д.).

## Текущие протоколы

| Протокол        | Базовый путь                    | Формат | Авторизация                          |
|-----------------|----------------------------------|--------|--------------------------------------|
| MyCompany       | `api/mycompany/transfers/v1`     | JSON   | `X-Api-Key` + `X-MyCompany-Signature` (HMAC тела) |
| Tillabuy        | `api/tillabuy`                   | XML    | По конфигу: `termid` (query) → `Tillabuy:Terminals` → `Tillabuy:Agents` → agentId |

Функции Tillabuy: `check` (prepare), `payment` (confirm/status), `getbalance`, `getrate`. Коды ошибок и тексты — в `TillabuyExtensions.Errors` / `ApiErrors` (протокол НКО).

Конфиг Tillabuy (пример в appsettings.json):
```json
"Tillabuy": {
  "Terminals": { "termid_from_request": "terminal_key" },
  "Agents": { "terminal_key": "agent_id" },
  "TermsCurrency": { "terminal_key": "TJS" }
}
```

## Как добавить новый протокол

1. **Contract**  
   В `Contract` добавьте папку/неймспейс под протокол (например `Contract.<Protocol>.Requests/Responses`) и DTO запросов/ответов в нужном формате (в т.ч. `[XmlRoot]`/`[XmlElement]` для XML).

2. **Маршруты**  
   Задайте базовый путь в атрибутах контроллера, например:
   - `[Route("api/mycompany/transfers/v1")]` — уже в `ApiEndpoints`.
   - `[Route("api/tillabuy")]` — для Tillabuy.
   - Для нового: `[Route("api/<протокол>")]`.

3. **Формат (JSON/XML)**  
   - JSON: по умолчанию, при необходимости `[Produces("application/json")]`, `[Consumes("application/json")]`.
   - XML: `[Produces("application/xml")]`, `[Consumes("application/xml")]` и при необходимости кастомный `OutputFormatter` (как для Tillabuy с `UseCustomXml`).

4. **Авторизация**  
   - Либо свой атрибут (как `SignatureAuthorize` для MyCompany), либо разрешение агента/терминала внутри экшена из заголовков/query/конфига (как в Tillabuy: termid → agent).

5. **Контроллер**  
   - Новый контроллер с выбранным route, Consumes/Produces и атрибутом авторизации (если есть).
   - Вызовы доменной логики через MediatR (те же команды/запросы Application), подставляя `agentId`/`terminalId`, полученные по правилам данного протокола.

6. **Swagger (опционально)**  
   В `Program.cs` при использовании Swagger добавьте группу для нового протокола и `ApiExplorerSettings(GroupName = "...")` на контроллере.

## Общая схема

- **Application** — не зависит от протокола: команды/запросы принимают `agentId`, `terminalId`, параметры перевода и т.д.
- **Контроллеры** — адаптеры протокола: парсят входящий запрос (JSON/XML), извлекают или определяют агента/терминал по правилам протокола, вызывают MediatR, приводят результат к формату ответа протокола (JSON/XML).

Таким образом добавление нового агента/протокола сводится к новому контроллеру, контрактам и при необходимости своему атрибуту авторизации и форматтеру.
