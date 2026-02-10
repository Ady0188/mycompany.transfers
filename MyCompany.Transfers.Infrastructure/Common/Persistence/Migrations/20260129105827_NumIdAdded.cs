using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NumIdAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "NumId",
                table: "Transfers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransferId",
                table: "Transfers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NumId",
                table: "Outboxes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransferId",
                table: "Outboxes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_NumId",
                table: "Transfers",
                column: "NumId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_NumId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "NumId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ProviderTransferId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "NumId",
                table: "Outboxes");

            migrationBuilder.DropColumn(
                name: "ProviderTransferId",
                table: "Outboxes");
        }
    }
}
