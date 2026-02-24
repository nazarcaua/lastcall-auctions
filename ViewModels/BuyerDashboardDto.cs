using System.Collections.Generic;
using System;
using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class BuyerDashboardDto
    {
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public List<BidDto> BidList { get; set; } = new();
        public List<AuctionBrowseDto> AuctionList { get; set; } = new();
        public List<object> Favourites { get; set; } = new();
        public List<object> Transactions { get; set; } = new();
    }

    public class BidDto
    {
        public long BidId { get; set; }
        public int AuctionId { get; set; }
        public decimal Amount { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public DateTime PlacedAt { get; set; }
    }
}
