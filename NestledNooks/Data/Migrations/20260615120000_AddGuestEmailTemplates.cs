using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddGuestEmailTemplates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[GuestEmailTemplates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [GuestEmailTemplates] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PropertySlug] nvarchar(120) NOT NULL,
                        [Category] nvarchar(40) NOT NULL,
                        [Title] nvarchar(120) NOT NULL,
                        [EmailSubject] nvarchar(200) NULL,
                        [Body] nvarchar(max) NOT NULL,
                        [SortOrder] int NOT NULL,
                        [UpdatedAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_GuestEmailTemplates] PRIMARY KEY ([Id])
                    );
                    CREATE INDEX [IX_GuestEmailTemplates_PropertySlug_SortOrder] ON [GuestEmailTemplates] ([PropertySlug], [SortOrder]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "GuestEmailTemplates");
        }
    }
}
