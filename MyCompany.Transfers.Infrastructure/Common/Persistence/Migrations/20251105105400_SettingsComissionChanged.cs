using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SettingsComissionChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeFlatMinor",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "FeePermille",
                table: "Services");

            migrationBuilder.AddColumn<long>(
                name: "FeeFlatMinor",
                table: "AgentServiceAccess",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "FeePermille",
                table: "AgentServiceAccess",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeeFlatMinor",
                table: "AgentServiceAccess");

            migrationBuilder.DropColumn(
                name: "FeePermille",
                table: "AgentServiceAccess");

            migrationBuilder.AddColumn<long>(
                name: "FeeFlatMinor",
                table: "Services",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "FeePermille",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
