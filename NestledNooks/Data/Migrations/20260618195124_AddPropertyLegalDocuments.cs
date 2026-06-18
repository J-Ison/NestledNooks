using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyLegalDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HouseRulesText",
                table: "RentalProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LegalDocumentsVersion",
                table: "RentalProperties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LiabilityAcknowledgmentText",
                table: "RentalProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RentalAgreementText",
                table: "RentalProperties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RequireGuestLegalAcceptance",
                table: "RentalProperties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BookingLegalAcceptanceJson",
                table: "BookingRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentLegalAcceptanceJson",
                table: "BookingPaymentLinks",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HouseRulesText",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "LegalDocumentsVersion",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "LiabilityAcknowledgmentText",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "RentalAgreementText",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "RequireGuestLegalAcceptance",
                table: "RentalProperties");

            migrationBuilder.DropColumn(
                name: "BookingLegalAcceptanceJson",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "PaymentLegalAcceptanceJson",
                table: "BookingPaymentLinks");
        }
    }
}
