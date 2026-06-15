using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddDirectBookingsEnabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('SiteSettings', 'DirectBookingsEnabled') IS NULL
                    ALTER TABLE [SiteSettings] ADD [DirectBookingsEnabled] bit NOT NULL
                        CONSTRAINT [DF_SiteSettings_DirectBookingsEnabled] DEFAULT (1);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DirectBookingsEnabled", table: "SiteSettings");
        }
    }
}
