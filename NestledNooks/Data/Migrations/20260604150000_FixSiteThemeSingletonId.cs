using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations;

/// <inheritdoc />
public partial class FixSiteThemeSingletonId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[SiteThemes]', N'U') IS NOT NULL
                DROP TABLE [SiteThemes];
            """);

        migrationBuilder.CreateTable(
            name: "SiteThemes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false),
                PresetKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                PrimaryColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PrimaryLightColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PrimarySoftBg = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PrimaryBorderColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PrimaryTextColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                AccentColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                AccentBorderColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                HeroStartColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                HeroMidColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                HeroEndColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                HeroBorderColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                BookingColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                BookingDarkColor = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PageBgTop = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                PageBgBottom = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiteThemes", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiteThemes");
    }
}
