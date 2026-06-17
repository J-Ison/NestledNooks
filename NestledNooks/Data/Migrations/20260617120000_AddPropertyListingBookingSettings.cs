using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddPropertyListingBookingSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('RentalProperties', 'MinimumNights') IS NULL
                    ALTER TABLE [RentalProperties] ADD [MinimumNights] int NOT NULL
                        CONSTRAINT [DF_RentalProperties_MinimumNights] DEFAULT (2);

                IF COL_LENGTH('RentalProperties', 'MinAdvanceBookingDays') IS NULL
                    ALTER TABLE [RentalProperties] ADD [MinAdvanceBookingDays] int NOT NULL
                        CONSTRAINT [DF_RentalProperties_MinAdvanceBookingDays] DEFAULT (10);

                IF COL_LENGTH('RentalProperties', 'MaxBookingDaysAhead') IS NULL
                    ALTER TABLE [RentalProperties] ADD [MaxBookingDaysAhead] int NOT NULL
                        CONSTRAINT [DF_RentalProperties_MaxBookingDaysAhead] DEFAULT (365);

                IF COL_LENGTH('RentalProperties', 'PetDepositPerTwoPets') IS NULL
                    ALTER TABLE [RentalProperties] ADD [PetDepositPerTwoPets] decimal(18,2) NOT NULL
                        CONSTRAINT [DF_RentalProperties_PetDepositPerTwoPets] DEFAULT (50);

                IF COL_LENGTH('SiteSettings', 'MinimumNights') IS NOT NULL
                BEGIN
                    UPDATE rp
                    SET
                        rp.[MinimumNights] = ss.[MinimumNights],
                        rp.[MinAdvanceBookingDays] = ss.[MinAdvanceBookingDays],
                        rp.[MaxBookingDaysAhead] = ss.[MaxBookingDaysAhead],
                        rp.[CleaningFee] = CASE WHEN ss.[CleaningFee] > 0 THEN ss.[CleaningFee] ELSE rp.[CleaningFee] END,
                        rp.[PetDepositPerTwoPets] = ss.[PetDepositPerTwoPets]
                    FROM [RentalProperties] rp
                    CROSS JOIN [SiteSettings] ss
                    WHERE ss.[Id] = 1 AND rp.[Slug] = 'deerfield-retreat';
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "MinimumNights", table: "RentalProperties");
            migrationBuilder.DropColumn(name: "MinAdvanceBookingDays", table: "RentalProperties");
            migrationBuilder.DropColumn(name: "MaxBookingDaysAhead", table: "RentalProperties");
            migrationBuilder.DropColumn(name: "PetDepositPerTwoPets", table: "RentalProperties");
        }
    }
}
