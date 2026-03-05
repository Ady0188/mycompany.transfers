using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecondAccountsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankIncomeAccount",
                table: "Terminals",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommissionAccount",
                table: "Providers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankIncomeAccount",
                table: "Terminals");

            migrationBuilder.DropColumn(
                name: "CommissionAccount",
                table: "Providers");
        }
    }
}
