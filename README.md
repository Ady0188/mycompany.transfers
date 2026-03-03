# MyCompany.Transfers (Clean)

Новая реализация сервиса переводов в отдельной папке. Протокол API (форматы запросов и ответов) **сохранён без изменений** для совместимости с текущими клиентами.

## Структура решения

- **MyCompany.Transfers.Domain** — сущности, value objects, DTO ответов, доменная логика (без комментариев и мёртвого кода).
- **MyCompany.Transfers.Contract** — маршруты API (ApiEndpoints), DTO запросов (PrepareRequest, ConfirmRequest, TransferMethod).
- **MyCompany.Transfers.Application** — слой приложения (будет: команды/запросы, хендлеры, валидаторы, интерфейсы).
- **MyCompany.Transfers.Infrastructure** — реализация персистенции и провайдеров (будет: EF Core, репозитории, HttpProviderSender).
- **MyCompany.Transfers.Api** — веб-API (будет: контроллеры, авторизация по подписи, единая обработка ошибок).

## Отличия от старого решения (в Domain)

- Исправлено имя: **ProviderServiceId** вместо ProviderServicveId (в Service и Outbox).
- В Transfer/Outbox убраны закомментированные блоки.
- В маппинге в DTO (ToPrepareResponseDto, ToConfirmResponseDto, ToStatusResponseDto) баланс агента берётся через **TryGetValue**, без риска InvalidOperationException.
- Добавлен статус **TECHNICAL** в маппинг TransferStatus → строка ответа (Helpers).
- Конструкторы и валидация домена без дублирования и лишних зависимостей.

## Текущий статус

- Реализованы: **структура решения** и полный слой **Domain** (включая DTO для протокола).
- Application, Infrastructure и Api пока заглушки; следующий шаг — реализация Application (интерфейсы, команды/запросы, хендлеры).
