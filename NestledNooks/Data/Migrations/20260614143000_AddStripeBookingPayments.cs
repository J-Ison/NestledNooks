using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NestledNooks.Data.Migrations
{
    public partial class AddStripeBookingPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('BookingRequests', 'RequiredDepositAmount') IS NULL
                    ALTER TABLE [BookingRequests] ADD [RequiredDepositAmount] decimal(18,2) NULL;

                IF COL_LENGTH('BookingRequests', 'DepositNonRefundable') IS NULL
                    ALTER TABLE [BookingRequests] ADD [DepositNonRefundable] bit NOT NULL CONSTRAINT [DF_BookingRequests_DepositNonRefundable] DEFAULT (0);

                IF OBJECT_ID(N'[BookingPaymentLinks]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [BookingPaymentLinks] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [BookingRequestId] int NOT NULL,
                        [Token] nvarchar(64) NOT NULL,
                        [Purpose] nvarchar(20) NOT NULL,
                        [Amount] decimal(18,2) NOT NULL,
                        [StripeCheckoutSessionId] nvarchar(200) NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [CompletedAtUtc] datetime2 NULL,
                        CONSTRAINT [PK_BookingPaymentLinks] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_BookingPaymentLinks_BookingRequests_BookingRequestId]
                            FOREIGN KEY ([BookingRequestId]) REFERENCES [BookingRequests] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_BookingPaymentLinks_BookingRequestId] ON [BookingPaymentLinks] ([BookingRequestId]);
                    CREATE UNIQUE INDEX [IX_BookingPaymentLinks_Token] ON [BookingPaymentLinks] ([Token]);
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BookingPaymentLinks");
            migrationBuilder.DropColumn(name: "RequiredDepositAmount", table: "BookingRequests");
            migrationBuilder.DropColumn(name: "DepositNonRefundable", table: "BookingRequests");
        }
    }
}
