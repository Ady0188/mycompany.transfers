namespace MyCompany.Transfers.Contract;

public class ApiEndpoints
{
    private const string BaseApi = "api/mycompany/transfers";

    public static class V1
    {
        private const string Version = $"{BaseApi}/v1";

        public static class MyCompanyTransfers
        {
            private const string Base = $"{Version}";

            public const string CheckAccount = $"{Base}/check";
            public const string Prepare = $"{Base}/prepare";
            public const string Confirm = $"{Base}/confirm";
            public const string GetStatus = $"{Base}/status";
            public const string GetBalance = $"{Base}/balance";
            public const string GetRate = $"{Base}/rates";
        }
    }
}
