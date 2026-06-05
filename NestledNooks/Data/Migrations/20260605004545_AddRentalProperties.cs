using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsHomepage = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StatsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagsLine1 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TagsLine2 = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BadgesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AboutText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmenitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuideTeaserText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookingSubtext = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BookingFinePrint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AirbnbUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VrboUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhotosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalProperties", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentalProperties_Slug",
                table: "RentalProperties",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalProperties");
        }
    }
}
