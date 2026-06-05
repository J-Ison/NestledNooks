using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAspNetUserProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('AspNetUsers', 'Nickname') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [Nickname] nvarchar(50) NULL;

                IF COL_LENGTH('AspNetUsers', 'MessageTagsJson') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [MessageTagsJson] nvarchar(500) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Idempotent repair migration — no down.
        }
    }
}
