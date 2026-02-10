using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SomeChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationCurrency",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "DestinationCurrency",
                table: "Outboxes");

            migrationBuilder.AlterColumn<long>(
                name: "CreditedMinor",
                table: "Transfers",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "CreditedCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<string>(
                name: "ProvReceivedParams",
                table: "Transfers",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderFeeCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProviderFeeMinor",
                table: "Transfers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderServicveId",
                table: "Services",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FeePermille",
                table: "Providers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<long>(
                name: "CreditedMinor",
                table: "Outboxes",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "CreditedCurrency",
                table: "Outboxes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<string>(
                name: "ProvReceivedParams",
                table: "Outboxes",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderFeeCurrency",
                table: "Outboxes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProviderFeeMinor",
                table: "Outboxes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderServicveId",
                table: "Outboxes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvReceivedParams",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProviderFeeCurrency",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProviderFeeMinor",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProviderServicveId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "FeePermille",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ProvReceivedParams",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "ProviderFeeCurrency",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "ProviderFeeMinor",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "ProviderServicveId",
                table: "Outboxes");

            migrationBuilder.AlterColumn<long>(
                name: "CreditedMinor",
                table: "Transfers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreditedCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationCurrency",
                table: "Transfers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<long>(
                name: "CreditedMinor",
                table: "Outboxes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreditedCurrency",
                table: "Outboxes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationCurrency",
                table: "Outboxes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
