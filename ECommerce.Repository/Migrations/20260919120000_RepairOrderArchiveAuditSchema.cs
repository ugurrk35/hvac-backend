using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Repository.Migrations;

[Migration("20260919120000_RepairOrderArchiveAuditSchema")]
public partial class RepairOrderArchiveAuditSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Some existing databases recorded the archive migration without its DDL.
        // Keep this repair idempotent so both affected and clean environments can upgrade safely.
        migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchiveReason\" character varying(1000);");
        migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchivedAt\" timestamp with time zone;");
        migrationBuilder.Sql("ALTER TABLE \"Orders\" ADD COLUMN IF NOT EXISTS \"ArchivedByUserId\" integer;");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Orders_IsDeleted_ArchivedAt\" ON \"Orders\" (\"IsDeleted\", \"ArchivedAt\");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally no-op: this repair must not remove columns from databases
        // where the original archive migration created them correctly.
    }
}
