using IdentityService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260720230000_AddAuditEvents")]
public partial class AddAuditEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                EventType = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(1000)",
                    maxLength: 1000,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEvents", column => column.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_ActorUserId",
            table: "AuditEvents",
            column: "ActorUserId");
        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_CreatedAt",
            table: "AuditEvents",
            column: "CreatedAt");
        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_EventType",
            table: "AuditEvents",
            column: "EventType");
        migrationBuilder.CreateIndex(
            name: "IX_AuditEvents_TargetUserId",
            table: "AuditEvents",
            column: "TargetUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "AuditEvents");
}
