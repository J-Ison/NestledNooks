using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelPriceSettingsToRentalProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AirbnbCleaningFee",
                table: "RentalProperties",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AirbnbGuestServiceFeePercent",
                table: "RentalProperties",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VrboCleaningFee",
                table: "RentalProperties",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VrboGuestServiceFeePercent",
                table: "RentalProperties",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VrboOccupancyTaxPercent",
                table: "RentalProperties",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirbnbCleaningFee",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "AirbnbGuestServiceFeePercent",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "VrboCleaningFee",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "VrboGuestServiceFeePercent",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "VrboOccupancyTaxPercent",
                table: "RentalProperties");
        }
    }
}
