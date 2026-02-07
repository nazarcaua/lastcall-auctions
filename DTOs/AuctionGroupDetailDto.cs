using System;
using System.Collections.Generic;

namespace LastCallMotorAuctions.API.DTOs
{
    public class AuctionGroupDetailDto
    {
        public int AuctionGroupId { get; set; }
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<AuctionBrowseDto> Auctions { get; set; } = new();
    }
}