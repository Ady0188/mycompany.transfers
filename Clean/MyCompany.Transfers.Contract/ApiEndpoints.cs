namespace MyCompany.Transfers.Contract;

/// <summary>
/// Базовые пути и эндпоинты по протоколам. Добавление нового протокола — новый класс/константы и контроллер в Api.
/// </summary>
public static class ApiEndpoints
{
    // MyCompany: JSON, авторизация X-Api-Key + X-MyCompany-Signature
    private const string BaseApi = "api/mycompany/transfers";

    // Tillabuy: XML, авторизация по конфигу (termid → agent)
    public const string TillabuyBase = "api";

    public static class V1
    {
        private const string Version = $"{BaseApi}/v1";

        public static class MyCompanyTransfers
        {
            private const string Base = Version;

            public const string CheckAccount = $"{Base}/check";
            public const string Prepare = $"{Base}/prepare";
            public const string Confirm = $"{Base}/confirm";
            public const string GetStatus = $"{Base}/status";
            public const string GetBalance = $"{Base}/balance";
            public const string GetRate = $"{Base}/rates";
        }
    }
}
