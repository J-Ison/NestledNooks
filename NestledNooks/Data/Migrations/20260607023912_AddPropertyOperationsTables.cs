using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyOperationsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyCustomFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyCustomFields", x => x.Id);
                });

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
                name: "IX_PropertyContacts_PropertySlug",
                table: "PropertyContacts",
                column: "PropertySlug");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyContacts_PropertySlug_SortOrder",
                table: "PropertyContacts",
                columns: new[] { "PropertySlug", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCustomFields_PropertySlug",
                table: "PropertyCustomFields",
                column: "PropertySlug");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyCustomFields_PropertySlug_SortOrder",
                table: "PropertyCustomFields",
                columns: new[] { "PropertySlug", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyOperationsProfiles_PropertySlug",
                table: "PropertyOperationsProfiles",
                column: "PropertySlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyContacts");

            migrationBuilder.DropTable(
                name: "PropertyCustomFields");

            migrationBuilder.DropTable(
                name: "PropertyOperationsProfiles");
        }
    }
}
