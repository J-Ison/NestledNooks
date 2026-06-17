using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddListingBookingSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('SiteSettings', 'MinimumNights') IS NULL
                    ALTER TABLE [SiteSettings] ADD [MinimumNights] int NOT NULL
                        CONSTRAINT [DF_SiteSettings_MinimumNights] DEFAULT (2);

                IF COL_LENGTH('SiteSettings', 'MinAdvanceBookingDays') IS NULL
                    ALTER TABLE [SiteSettings] ADD [MinAdvanceBookingDays] int NOT NULL
                        CONSTRAINT [DF_SiteSettings_MinAdvanceBookingDays] DEFAULT (10);

                IF COL_LENGTH('SiteSettings', 'MaxBookingDaysAhead') IS NULL
                    ALTER TABLE [SiteSettings] ADD [MaxBookingDaysAhead] int NOT NULL
                        CONSTRAINT [DF_SiteSettings_MaxBookingDaysAhead] DEFAULT (365);

                IF COL_LENGTH('SiteSettings', 'CleaningFee') IS NULL
                    ALTER TABLE [SiteSettings] ADD [CleaningFee] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_SiteSettings_CleaningFee] DEFAULT (200);

                IF COL_LENGTH('SiteSettings', 'PetDepositPerTwoPets') IS NULL
                    ALTER TABLE [SiteSettings] ADD [PetDepositPerTwoPets] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_SiteSettings_PetDepositPerTwoPets] DEFAULT (50);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MinimumNights", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MinAdvanceBookingDays", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "MaxBookingDaysAhead", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "CleaningFee", table: "SiteSettings");
            migrationBuilder.DropColumn(name: "PetDepositPerTwoPets", table: "SiteSettings");
        }
    }
}
