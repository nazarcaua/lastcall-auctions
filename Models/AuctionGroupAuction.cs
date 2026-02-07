namespace LastCallMotorAuctions.API.Models
{
    public class AuctionGroupAuction
    {
        public int AuctionGroupId { get; set; }
        public AuctionGroup? AuctionGroup { get; set; }

        public int AuctionId { get; set; }
        public Auction? Auction { get; set; }
    }
}
