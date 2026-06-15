using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddAdminNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('AspNetUsers', 'RegisteredAtUtc') IS NULL
                    ALTER TABLE [AspNetUsers] ADD [RegisteredAtUtc] datetime2 NOT NULL
                        CONSTRAINT [DF_AspNetUsers_RegisteredAtUtc] DEFAULT ('2000-01-01T00:00:00');

                IF OBJECT_ID(N'[AdminBookingSeens]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AdminBookingSeens] (
                        [UserId] nvarchar(450) NOT NULL,
                        [BookingRequestId] int NOT NULL,
                        [SeenAtUtc] datetime2 NOT NULL,
                        CONSTRAINT [PK_AdminBookingSeens] PRIMARY KEY ([UserId], [BookingRequestId]),
                        CONSTRAINT [FK_AdminBookingSeens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_AdminBookingSeens_BookingRequests_BookingRequestId] FOREIGN KEY ([BookingRequestId]) REFERENCES [BookingRequests] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_AdminBookingSeens_BookingRequestId] ON [AdminBookingSeens] ([BookingRequestId]);
                END

                IF OBJECT_ID(N'[AdminUserNotificationStates]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [AdminUserNotificationStates] (
                        [UserId] nvarchar(450) NOT NULL,
                        [UsersSectionSeenAtUtc] datetime2 NULL,
                        CONSTRAINT [PK_AdminUserNotificationStates] PRIMARY KEY ([UserId]),
                        CONSTRAINT [FK_AdminUserNotificationStates_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
                    );
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AdminBookingSeens");
            migrationBuilder.DropTable(name: "AdminUserNotificationStates");
            migrationBuilder.DropColumn(name: "RegisteredAtUtc", table: "AspNetUsers");
        }
    }
}
