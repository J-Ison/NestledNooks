using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBookingRequestsForDirectBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingNumber",
                table: "BookingRequests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CleaningFee",
                table: "BookingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NightCount",
                table: "BookingRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "NightlyRate",
                table: "BookingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PetFee",
                table: "BookingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "StatusNote",
                table: "BookingRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusUpdatedAtUtc",
                table: "BookingRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "BookingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "BookingRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "BookingRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExternalCalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalCalendarEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_BookingNumber",
                table: "BookingRequests",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_PropertySlug_CheckIn_CheckOut",
                table: "BookingRequests",
                columns: new[] { "PropertySlug", "CheckIn", "CheckOut" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_Status",
                table: "BookingRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_UserId",
                table: "BookingRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalCalendarEvents_PropertySlug_StartDate_EndDate",
                table: "ExternalCalendarEvents",
                columns: new[] { "PropertySlug", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookingRequests_AspNetUsers_UserId",
                table: "BookingRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingRequests_AspNetUsers_UserId",
                table: "BookingRequests");

            migrationBuilder.DropTable(
                name: "ExternalCalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_BookingNumber",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_PropertySlug_CheckIn_CheckOut",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_Status",
                table: "BookingRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingRequests_UserId",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "BookingNumber",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "CleaningFee",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "NightCount",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "NightlyRate",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "PetFee",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "StatusNote",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "StatusUpdatedAtUtc",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "BookingRequests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BookingRequests");
        }
    }
}
