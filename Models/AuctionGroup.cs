using System;
using System.Collections.Generic;

namespace LastCallMotorAuctions.API.Models
{
    public class AuctionGroup
    {
        public int AuctionGroupId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AuctionGroupAuction> Auctions { get; set; } = new List<AuctionGroupAuction>();
    }
}
