using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyNightlyRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[PropertyNightlyRates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [PropertyNightlyRates] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PropertySlug] nvarchar(120) NOT NULL,
                        [Date] date NOT NULL,
                        [Rate] decimal(18,2) NOT NULL,
                        [MinimumStay] int NULL,
                        [UpdatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_PropertyNightlyRates] PRIMARY KEY ([Id])
                    );
                    CREATE UNIQUE INDEX [IX_PropertyNightlyRates_PropertySlug_Date]
                        ON [PropertyNightlyRates] ([PropertySlug], [Date]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyNightlyRates");
        }
    }
}
