using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AccountDefinitions",
                columns: new[] { "Id", "Algorithm", "Code", "MaxLength", "MinLength", "Normalize", "Regex" },
                values: new object[,]
                {
                    { new Guid("1744710c-a062-4c33-8077-756db29a06b6"), 0, "PHONE", 15, 9, 2, "^\\d{9,15}$" },
                    { new Guid("3fcdac91-5f18-425b-945f-169d539b6bde"), 1, "PAN", 16, 16, 2, "^\\d{16}$" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AccountDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("1744710c-a062-4c33-8077-756db29a06b6"));

            migrationBuilder.DeleteData(
                table: "AccountDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("3fcdac91-5f18-425b-945f-169d539b6bde"));
        }
    }
}
