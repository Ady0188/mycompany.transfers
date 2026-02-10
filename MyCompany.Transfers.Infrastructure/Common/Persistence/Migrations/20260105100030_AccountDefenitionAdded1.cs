using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AccountDefenitionAdded1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_account_definitions",
                table: "account_definitions");

            migrationBuilder.RenameTable(
                name: "account_definitions",
                newName: "AccountDefinitions");

            migrationBuilder.RenameIndex(
                name: "IX_account_definitions_Code",
                table: "AccountDefinitions",
                newName: "IX_AccountDefinitions_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountDefinitions",
                table: "AccountDefinitions",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountDefinitions",
                table: "AccountDefinitions");

            migrationBuilder.RenameTable(
                name: "AccountDefinitions",
                newName: "account_definitions");

            migrationBuilder.RenameIndex(
                name: "IX_AccountDefinitions_Code",
                table: "account_definitions",
                newName: "IX_account_definitions_Code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_account_definitions",
                table: "account_definitions",
                column: "Id");
        }
    }
}
