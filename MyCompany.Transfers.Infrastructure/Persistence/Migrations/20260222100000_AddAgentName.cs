using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [Migration("20260222100000_AddAgentName")]
    public partial class AddAgentName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Agents",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Name", table: "Agents");
        }
    }
}
