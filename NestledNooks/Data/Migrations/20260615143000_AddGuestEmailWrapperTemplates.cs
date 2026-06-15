using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddGuestEmailWrapperTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('SiteSettings', 'GuestEmailHeaderTemplate') IS NULL
                    ALTER TABLE [SiteSettings] ADD [GuestEmailHeaderTemplate] nvarchar(2000) NULL;

                IF COL_LENGTH('SiteSettings', 'GuestEmailFooterTemplate') IS NULL
                    ALTER TABLE [SiteSettings] ADD [GuestEmailFooterTemplate] nvarchar(4000) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GuestEmailHeaderTemplate", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "GuestEmailFooterTemplate", table: "SiteSettings");
        }
    }
}
