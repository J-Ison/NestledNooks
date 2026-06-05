using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeerfieldGuestGuideQrCodeUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeerfieldGuestGuideQrCodeUrl",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeerfieldGuestGuideQrCodeUrl",
                table: "SiteSettings");
        }
    }
}
