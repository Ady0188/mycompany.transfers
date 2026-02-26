using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BalanceHistoryAndDailyBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentBalanceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DocId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CurrentBalanceMinor = table.Column<long>(type: "bigint", nullable: false),
                    IncomeMinor = table.Column<long>(type: "bigint", nullable: false),
                    NewBalanceMinor = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentBalanceHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentDailyBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OpeningBalanceMinor = table.Column<long>(type: "bigint", nullable: false),
                    ClosingBalanceMinor = table.Column<long>(type: "bigint", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDailyBalance", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_CreatedAtUtc",
                table: "AgentBalanceHistory",
                columns: new[] { "AgentId", "Currency", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_ReferenceType_ReferenceId",
                table: "AgentBalanceHistory",
                columns: new[] { "AgentId", "Currency", "ReferenceType", "ReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentDailyBalance_AgentId_Currency_Date_TimeZoneId_Scope",
                table: "AgentDailyBalance",
                columns: new[] { "AgentId", "Currency", "Date", "TimeZoneId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentDailyBalance_AgentId_Date",
                table: "AgentDailyBalance",
                columns: new[] { "AgentId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentBalanceHistory");

            migrationBuilder.DropTable(
                name: "AgentDailyBalance");
        }
    }
}
