using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyCompany.Transfers.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddedFiledForAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Добавляем колонку (безопасно: только если её ещё нет)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Agents'
                          AND column_name = 'Name'
                    ) THEN
                        ALTER TABLE ""Agents""
                        ADD COLUMN ""Name"" character varying(256) NOT NULL DEFAULT '';
                    END IF;
                END $$;
                ");

            // 2) Добавляем запись в __EFMigrationsHistory (тоже безопасно: только если нет)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
                    ) AND NOT EXISTS (
                        SELECT 1 FROM ""__EFMigrationsHistory""
                        WHERE ""MigrationId"" = '20260222100000_AddAgentName'
                    ) THEN
                        INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                        VALUES ('20260222100000_AddAgentName', '10.0.3');
                    END IF;
                END $$;
                "); 
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат: удаляем колонку, и удаляем запись из истории (если нужно)
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = 'Agents'
                          AND column_name = 'Name'
                    ) THEN
                        ALTER TABLE ""Agents"" DROP COLUMN ""Name"";
                    END IF;
                END $$;
                ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables
                        WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'
                    ) THEN
                        DELETE FROM ""__EFMigrationsHistory""
                        WHERE ""MigrationId"" = '20260222100000_AddAgentName';
                    END IF;
                END $$;
                ");
        }
    }
}
