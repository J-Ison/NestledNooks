using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddExternalCalendarTrustDays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('RentalProperties', 'ExternalCalendarTrustDays') IS NULL
                    ALTER TABLE [RentalProperties] ADD [ExternalCalendarTrustDays] int NOT NULL
                        CONSTRAINT [DF_RentalProperties_ExternalCalendarTrustDays] DEFAULT (180);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ExternalCalendarTrustDays", table: "RentalProperties");
        }
    }
}
