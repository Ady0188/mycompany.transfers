using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ParameteraTableAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceParamDefinition",
                table: "ServiceParamDefinition");

            migrationBuilder.DropIndex(
                name: "IX_ServiceParamDefinition_ServiceId",
                table: "ServiceParamDefinition");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ServiceParamDefinition");

            migrationBuilder.DropColumn(
                name: "Regex",
                table: "ServiceParamDefinition");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "ServiceParamDefinition",
                newName: "ParameterId");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceId",
                table: "ServiceParamDefinition",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "Required",
                table: "ServiceParamDefinition",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceParamDefinition",
                table: "ServiceParamDefinition",
                columns: new[] { "ServiceId", "ParameterId" });

            migrationBuilder.CreateTable(
                name: "ParamDefinition",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Regex = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParamDefinition", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceParamDefinition_ParameterId",
                table: "ServiceParamDefinition",
                column: "ParameterId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceParamDefinition_ParamDefinition_ParameterId",
                table: "ServiceParamDefinition",
                column: "ParameterId",
                principalTable: "ParamDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceParamDefinition_ParamDefinition_ParameterId",
                table: "ServiceParamDefinition");

            migrationBuilder.DropTable(
                name: "ParamDefinition");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceParamDefinition",
                table: "ServiceParamDefinition");

            migrationBuilder.DropIndex(
                name: "IX_ServiceParamDefinition_ParameterId",
                table: "ServiceParamDefinition");

            migrationBuilder.RenameColumn(
                name: "ParameterId",
                table: "ServiceParamDefinition",
                newName: "Code");

            migrationBuilder.AlterColumn<bool>(
                name: "Required",
                table: "ServiceParamDefinition",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ServiceId",
                table: "ServiceParamDefinition",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "ServiceParamDefinition",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Regex",
                table: "ServiceParamDefinition",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceParamDefinition",
                table: "ServiceParamDefinition",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceParamDefinition_ServiceId",
                table: "ServiceParamDefinition",
                column: "ServiceId");
        }
    }
}
