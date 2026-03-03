using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SidDataForParamDefenitionAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ParamDefinition",
                columns: new[] { "Id", "Active", "Code", "Description", "Name", "Regex" },
                values: new object[,]
                {
                    { "100", true, "sender_doc_type", null, "Тип удостоверяющего документа плательщика", null },
                    { "101", true, "sender_doc_number", null, "Серия и номер документа", "^[A-Z0-9]+$" },
                    { "102", true, "sender_phone", null, "Номер мобильного телефона плательщика", null },
                    { "103", true, "sender_doc_department_code", null, "Код подразделения, выдавшего паспорт", null },
                    { "104", true, "sender_residency", null, "Резидентство плательщика: 1 – резидент РТ, 0 – нерезидент.", "^(1{1}|2{1})$" },
                    { "105", true, "account_number", null, "Номер счёта плательщика", null },
                    { "106", true, "sender_doc_issue_date", null, "Дата выдачи документа", "^(19|20)\\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$" },
                    { "107", true, "sender_fullname_cyr", null, "Полное ФИО плательщика (кириллицей)", "^[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*(?:\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*)?$" },
                    { "108", true, "sender_firstname_cyr", null, "Фамилия плательщика (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "109", true, "sender_lastname_cyr", null, "Имя плательщика (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "110", true, "sender_middlename_cyr", null, "Отчество плательщика (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "111", true, "receiver_fullname_cyr", null, "Полное ФИО получателя (кириллицей)", "^[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*(?:\\s[А-ЯЁа-яё]+(?:-[А-ЯЁа-яё]+)*)?$" },
                    { "112", true, "receiver_firstname_cyr", null, "Имя получателя (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "113", true, "receiver_lastname_cyr", null, "Фамилия получателя (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "114", true, "receiver_middlename_cyr", null, "Отчество получателя (кириллицей)", "^[А-Яа-яЁё\\s\\-]+$" },
                    { "115", true, "sender_fullname", null, "Полное ФИО плательщика \"Фамилия Имя Отчество\"", "^[A-Za-z]+(?:-[A-Za-z]+)*\\s[A-Za-z]+(?:-[A-Za-z]+)*(?:\\s[A-Za-z]+(?:-[A-Za-z]+)*)?$" },
                    { "116", true, "sender_lastname", null, "Фамилия плательщика", "^[A-Za-z\\s\\-]+$" },
                    { "117", true, "sender_firstname", null, "Имя плательщика", "^[A-Za-z\\s\\-]+$" },
                    { "118", true, "sender_middlename", null, "Отчество плательщика", "^[A-Za-z\\s\\-]+$" },
                    { "119", true, "sender_doc_issuer", null, "Орган, выдавший документ", null },
                    { "120", true, "sender_birth_place", null, "Место рождения плательщика", null },
                    { "121", true, "sender_citizenship", null, "Гражданство плательщика", null },
                    { "122", true, "sender_registration_address", null, "Адрес регистрации: страна, регион, город, улица, дом, квартира и т.п.", null },
                    { "123", true, "receiver_fullname", null, "Полное ФИО получателя", "^[A-Za-z]+(?:-[A-Za-z]+)*\\s[A-Za-z]+(?:-[A-Za-z]+)*(?:\\s[A-Za-z]+(?:-[A-Za-z]+)*)?$" },
                    { "124", true, "receiver_firstname", null, "Имя получателя", "^[A-Za-z\\s\\-]+$" },
                    { "125", true, "receiver_lastname", null, "Фамилия получателя", "^[A-Za-z\\s\\-]+$" },
                    { "126", true, "receiver_middlename", null, "Отчество получателя", "^[A-Za-z\\s\\-]+$" },
                    { "127", true, "sender_birth_date", null, "Дата рождения плательщика", "^(19|20)\\d{2}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])$" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "100");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "101");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "102");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "103");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "104");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "105");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "106");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "107");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "108");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "109");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "110");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "111");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "112");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "113");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "114");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "115");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "116");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "117");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "118");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "119");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "120");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "121");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "122");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "123");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "124");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "125");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "126");

            migrationBuilder.DeleteData(
                table: "ParamDefinition",
                keyColumn: "Id",
                keyValue: "127");
        }
    }
}
