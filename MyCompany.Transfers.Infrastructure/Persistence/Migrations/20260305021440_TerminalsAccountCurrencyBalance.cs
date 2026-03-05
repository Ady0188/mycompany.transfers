using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TerminalsAccountCurrencyBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Terminals: добавить счёт, валюту и баланс
            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Terminals",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Terminals",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<long>(
                name: "BalanceMinor",
                table: "Terminals",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // AgentBalanceHistory: добавить TerminalId (nullable для существующих записей)
            migrationBuilder.AddColumn<string>(
                name: "TerminalId",
                table: "AgentBalanceHistory",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // AgentDailyBalance: добавить TerminalId (nullable для существующих)
            migrationBuilder.AddColumn<string>(
                name: "TerminalId",
                table: "AgentDailyBalance",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Удалить старые индексы AgentBalanceHistory
            migrationBuilder.DropIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_ReferenceType_ReferenceId",
                table: "AgentBalanceHistory");
            migrationBuilder.DropIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_CreatedAtUtc",
                table: "AgentBalanceHistory");

            // Удалить старые индексы AgentDailyBalance
            migrationBuilder.DropIndex(
                name: "IX_AgentDailyBalance_AgentId_Currency_Date_TimeZoneId_Scope",
                table: "AgentDailyBalance");

            // Удалить Account и Balances у Agents
            migrationBuilder.DropColumn(
                name: "Account",
                table: "Agents");
            migrationBuilder.DropColumn(
                name: "Balances",
                table: "Agents");

            // Новые индексы AgentBalanceHistory по TerminalId
            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_TerminalId_ReferenceType_ReferenceId",
                table: "AgentBalanceHistory",
                columns: new[] { "TerminalId", "ReferenceType", "ReferenceId" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_TerminalId_CreatedAtUtc",
                table: "AgentBalanceHistory",
                columns: new[] { "TerminalId", "CreatedAtUtc" });

            // Новый уникальный индекс AgentDailyBalance по TerminalId
            migrationBuilder.CreateIndex(
                name: "IX_AgentDailyBalance_TerminalId_Date_TimeZoneId_Scope",
                table: "AgentDailyBalance",
                columns: new[] { "TerminalId", "Date", "TimeZoneId", "Scope" },
                unique: true);

            // Сделать TerminalId обязательным после бэкфилла (опционально: раскомментировать после заполнения)
            // migrationBuilder.AlterColumn<string>(name: "TerminalId", table: "AgentBalanceHistory", nullable: false);
            // migrationBuilder.AlterColumn<string>(name: "TerminalId", table: "AgentDailyBalance", nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentBalanceHistory_TerminalId_ReferenceType_ReferenceId",
                table: "AgentBalanceHistory");
            migrationBuilder.DropIndex(
                name: "IX_AgentBalanceHistory_TerminalId_CreatedAtUtc",
                table: "AgentBalanceHistory");
            migrationBuilder.DropIndex(
                name: "IX_AgentDailyBalance_TerminalId_Date_TimeZoneId_Scope",
                table: "AgentDailyBalance");

            migrationBuilder.AddColumn<string>(
                name: "Account",
                table: "Agents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");
            migrationBuilder.AddColumn<string>(
                name: "Balances",
                table: "Agents",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_ReferenceType_ReferenceId",
                table: "AgentBalanceHistory",
                columns: new[] { "AgentId", "Currency", "ReferenceType", "ReferenceId" },
                unique: true);
            migrationBuilder.CreateIndex(
                name: "IX_AgentBalanceHistory_AgentId_Currency_CreatedAtUtc",
                table: "AgentBalanceHistory",
                columns: new[] { "AgentId", "Currency", "CreatedAtUtc" });
            migrationBuilder.CreateIndex(
                name: "IX_AgentDailyBalance_AgentId_Currency_Date_TimeZoneId_Scope",
                table: "AgentDailyBalance",
                columns: new[] { "AgentId", "Currency", "Date", "TimeZoneId", "Scope" },
                unique: true);

            migrationBuilder.DropColumn(name: "Account", table: "Terminals");
            migrationBuilder.DropColumn(name: "Currency", table: "Terminals");
            migrationBuilder.DropColumn(name: "BalanceMinor", table: "Terminals");
            migrationBuilder.DropColumn(name: "TerminalId", table: "AgentBalanceHistory");
            migrationBuilder.DropColumn(name: "TerminalId", table: "AgentDailyBalance");
        }
    }
}
