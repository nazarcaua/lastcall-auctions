using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LastCallMotorAuctions.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionGroups",
                columns: table => new
                {
                    AuctionGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionGroups", x => x.AuctionGroupId);
                });

            migrationBuilder.CreateTable(
                name: "AuctionGroupAuctions",
                columns: table => new
                {
                    AuctionGroupId = table.Column<int>(type: "int", nullable: false),
                    AuctionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionGroupAuctions", x => new { x.AuctionGroupId, x.AuctionId });
                    table.ForeignKey(
                        name: "FK_AuctionGroupAuctions_AuctionGroups_AuctionGroupId",
                        column: x => x.AuctionGroupId,
                        principalTable: "AuctionGroups",
                        principalColumn: "AuctionGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuctionGroupAuctions_Auctions_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "Auctions",
                        principalColumn: "AuctionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionGroupAuctions_AuctionId",
                table: "AuctionGroupAuctions",
                column: "AuctionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionGroupAuctions");

            migrationBuilder.DropTable(
                name: "AuctionGroups");
        }
    }
}
