using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OutboxAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorDescription",
                table: "Transfers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderCode",
                table: "Transfers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outboxes",
                columns: table => new
                {
                    TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TerminalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Account = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DestinationCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreditedMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreditedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExchangeRate = table.Column<decimal>(type: "numeric(20,8)", nullable: true),
                    RateTimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ErrorDescription = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    QuoteId = table.Column<string>(type: "text", nullable: true),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: true),
                    TotalCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    FeeMinor = table.Column<long>(type: "bigint", nullable: true),
                    FeeCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    QuoteExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreparedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outboxes", x => x.TransferId);
                });

            migrationBuilder.CreateTable(
                name: "ProviderErrorCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ErrorCode = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderErrorCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Outboxes_AgentId_ExternalId",
                table: "Outboxes",
                columns: new[] { "AgentId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderErrorCodes_ProviderId_ProviderCode",
                table: "ProviderErrorCodes",
                columns: new[] { "ProviderId", "ProviderCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Outboxes");

            migrationBuilder.DropTable(
                name: "ProviderErrorCodes");

            migrationBuilder.DropColumn(
                name: "ErrorDescription",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProviderCode",
                table: "Transfers");
        }
    }
}
