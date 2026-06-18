using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShowChannelPriceLinksToRentalProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowChannelPriceLinks",
                table: "RentalProperties",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowChannelPriceLinks",
                table: "RentalProperties");
        }
    }
}
