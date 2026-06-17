using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddAllowFarAdvanceDirectBooking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('RentalProperties', 'AllowFarAdvanceDirectBooking') IS NULL
                    ALTER TABLE [RentalProperties] ADD [AllowFarAdvanceDirectBooking] bit NOT NULL
                        CONSTRAINT [DF_RentalProperties_AllowFarAdvanceDirectBooking] DEFAULT (1);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AllowFarAdvanceDirectBooking", table: "RentalProperties");
        }
    }
}
