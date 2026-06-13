using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePropertyProfileWithEquipmentItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyEquipmentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Item = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyEquipmentItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyEquipmentItems_PropertySlug",
                table: "PropertyEquipmentItems",
                column: "PropertySlug");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyEquipmentItems_PropertySlug_SortOrder",
                table: "PropertyEquipmentItems",
                columns: new[] { "PropertySlug", "SortOrder" });

            migrationBuilder.Sql("""
                INSERT INTO PropertyEquipmentItems (PropertySlug, Item, Value, SortOrder, UpdatedAtUtc)
                SELECT p.PropertySlug, v.Item, v.Value, v.SortOrder, p.UpdatedAtUtc
                FROM PropertyOperationsProfiles p
                CROSS APPLY (VALUES
                    (N'Doorbell Camera', p.CamerasNotes, 0),
                    (N'Door Smartlock', p.SmartLockNotes, 1),
                    (N'Smart garage door', p.SmartGarageNotes, 2),
                    (N'Lockbox', p.BackupLockboxCode, 3)
                ) v(Item, Value, SortOrder)
                WHERE v.Value IS NOT NULL AND LTRIM(RTRIM(v.Value)) <> N'';
                """);

            migrationBuilder.DropTable(
                name: "PropertyOperationsProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyOperationsProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CamerasNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SmartLockNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SmartGarageNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BackupLockboxCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyOperationsProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyOperationsProfiles_PropertySlug",
                table: "PropertyOperationsProfiles",
                column: "PropertySlug",
                unique: true);

            migrationBuilder.DropTable(
                name: "PropertyEquipmentItems");
        }
    }
}
