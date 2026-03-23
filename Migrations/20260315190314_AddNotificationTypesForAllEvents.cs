using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastCallMotorAuctions.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTypesForAllEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "NotificationTypes",
                 columns: new[] { "TypeId", "Name" },
                 values: new object[,]
                {
                    { (short)6, "AuctionWon" },
                    { (short)7, "AuctionEndingSoon" },
                    { (short)8, "SellerRequestApproved" },
                    { (short)9, "SellerRequestRejected" },
                    { (short)10, "ListingApproved" },
                    { (short)11, "ListingRejected" },
                    { (short)12, "AuctionStarted" },
                    { (short)14, "NewBid" },
                    { (short)15, "PaymentPreauthSuccess" },
                    { (short)16, "PaymentPreauthFailed" }
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "NotificationTypes",
                keyColumn: "TypeId",
                keyValues: new object[] { (short)6, (short)7, (short)8, (short)9, (short)10, (short)11, (short)12, (short)14, (short)15, (short)16 });
                    }
    }
}
