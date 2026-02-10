using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountDefenitionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountDefinitionId",
                table: "Services",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "account_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Regex = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Normalize = table.Column<int>(type: "integer", nullable: false),
                    Algorithm = table.Column<int>(type: "integer", nullable: false),
                    MinLength = table.Column<int>(type: "integer", nullable: true),
                    MaxLength = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_definitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_definitions_Code",
                table: "account_definitions",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_definitions");

            migrationBuilder.DropColumn(
                name: "AccountDefinitionId",
                table: "Services");
        }
    }
}
