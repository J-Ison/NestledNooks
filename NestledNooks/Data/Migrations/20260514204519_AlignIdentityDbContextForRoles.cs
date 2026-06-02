using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Migrations
{
    /// <inheritdoc />
    public partial class AlignIdentityDbContextForRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySlug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GuestFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GuestPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    PetCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRequests_CreatedAtUtc",
                table: "BookingRequests",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRequests");
        }
    }
}
