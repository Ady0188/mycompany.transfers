using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FxRateAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreditedCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "CreditedMinor",
                table: "Transfers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "DestinationCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Transfers",
                type: "numeric(20,8)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RateTimestampUtc",
                table: "Transfers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FxRounding",
                table: "Services",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutCurrency",
                table: "Services",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FxRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_BaseCurrency_QuoteCurrency",
                table: "FxRates",
                columns: new[] { "BaseCurrency", "QuoteCurrency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FxRates");

            migrationBuilder.DropColumn(
                name: "CreditedCurrency",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "CreditedMinor",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "DestinationCurrency",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "RateTimestampUtc",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "FxRounding",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PayoutCurrency",
                table: "Services");
        }
    }
}
