using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Innitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Regex = table.Column<string>(type: "text", nullable: true),
                    Normalize = table.Column<int>(type: "integer", nullable: false),
                    Algorithm = table.Column<int>(type: "integer", nullable: false),
                    MinLength = table.Column<int>(type: "integer", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentCurrencyAccess",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCurrencyAccess", x => new { x.AgentId, x.Currency });
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Account = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Asia/Dushanbe"),
                    Balances = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentServiceAccess",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    FeePermille = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FeeFlatMinor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentServiceAccess", x => new { x.AgentId, x.ServiceId });
                });

            migrationBuilder.CreateTable(
                name: "FxRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BaseCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    QuoteCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(20,8)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "manual"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outboxes",
                columns: table => new
                {
                    TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TerminalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderServiceId = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Account = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProviderTransferId = table.Column<string>(type: "text", nullable: true),
                    ProviderCode = table.Column<string>(type: "text", nullable: true),
                    ErrorDescription = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    QuoteId = table.Column<string>(type: "text", nullable: true),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: true),
                    TotalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    FeeMinor = table.Column<long>(type: "bigint", nullable: true),
                    FeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ProviderFeeMinor = table.Column<long>(type: "bigint", nullable: true),
                    ProviderFeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CreditedMinor = table.Column<long>(type: "bigint", nullable: true),
                    CreditedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,8)", nullable: true),
                    RateTimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuoteExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    ProvReceivedParams = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outboxes", x => x.TransferId);
                });

            migrationBuilder.CreateTable(
                name: "ParamDefinition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Regex = table.Column<string>(type: "text", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParamDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Account = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    AuthType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FeePermille = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderServiceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AllowedCurrencies = table.Column<string>(type: "text", nullable: false),
                    FxRounding = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    MinAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    MaxAmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    AccountDefinitionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terminals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terminals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TerminalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Account = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ProviderTransferId = table.Column<string>(type: "text", nullable: true),
                    ProviderCode = table.Column<string>(type: "text", nullable: true),
                    ErrorDescription = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    QuoteId = table.Column<string>(type: "text", nullable: true),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: true),
                    TotalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    FeeMinor = table.Column<long>(type: "bigint", nullable: true),
                    FeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ProviderFeeMinor = table.Column<long>(type: "bigint", nullable: true),
                    ProviderFeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CreditedMinor = table.Column<long>(type: "bigint", nullable: true),
                    CreditedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,8)", nullable: true),
                    RateTimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    QuoteExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    ProvReceivedParams = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceParamDefinition",
                columns: table => new
                {
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ParameterId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceParamDefinition", x => new { x.ServiceId, x.ParameterId });
                    table.ForeignKey(
                        name: "FK_ServiceParamDefinition_ParamDefinition_ParameterId",
                        column: x => x.ParameterId,
                        principalTable: "ParamDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceParamDefinition_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_AgentId_BaseCurrency_QuoteCurrency",
                table: "FxRates",
                columns: new[] { "AgentId", "BaseCurrency", "QuoteCurrency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outboxes_AgentId_ExternalId",
                table: "Outboxes",
                columns: new[] { "AgentId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceParamDefinition_ParameterId",
                table: "ServiceParamDefinition",
                column: "ParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_Terminals_ApiKey",
                table: "Terminals",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_AgentId_ExternalId",
                table: "Transfers",
                columns: new[] { "AgentId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_NumId",
                table: "Transfers",
                column: "NumId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountDefinitions");

            migrationBuilder.DropTable(
                name: "AgentCurrencyAccess");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "AgentServiceAccess");

            migrationBuilder.DropTable(
                name: "FxRates");

            migrationBuilder.DropTable(
                name: "Outboxes");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "ServiceParamDefinition");

            migrationBuilder.DropTable(
                name: "Terminals");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropTable(
                name: "ParamDefinition");

            migrationBuilder.DropTable(
                name: "Services");
        }
    }
}
