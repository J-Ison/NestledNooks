using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyBookingDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscountsJson",
                table: "RentalProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountsJson",
                table: "RentalProperties");
        }
    }
}
