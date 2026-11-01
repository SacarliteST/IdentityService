using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "InviteCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            // Перенос уже заданных названий групп в справочник и привязка по имени.
            migrationBuilder.Sql("""
                INSERT INTO "Groups" ("Id", "Name", "CreatedAt")
                SELECT gen_random_uuid(), name, now() FROM (
                    SELECT DISTINCT "GroupName" AS name FROM "InviteCodes" WHERE "GroupName" IS NOT NULL
                    UNION
                    SELECT DISTINCT "GroupName" FROM "AspNetUsers" WHERE "GroupName" IS NOT NULL
                ) AS names;

                UPDATE "InviteCodes" AS code SET "GroupId" = grp."Id"
                FROM "Groups" AS grp WHERE code."GroupName" = grp."Name";

                UPDATE "AspNetUsers" AS usr SET "GroupId" = grp."Id"
                FROM "Groups" AS grp WHERE usr."GroupName" = grp."Name";
                """);

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_InviteCodes_GroupId",
                table: "InviteCodes",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GroupId",
                table: "AspNetUsers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_Name",
                table: "Groups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Groups_GroupId",
                table: "AspNetUsers",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InviteCodes_Groups_GroupId",
                table: "InviteCodes",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Groups_GroupId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_InviteCodes_Groups_GroupId",
                table: "InviteCodes");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_InviteCodes_GroupId",
                table: "InviteCodes");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GroupId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "InviteCodes");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "InviteCodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "AspNetUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
