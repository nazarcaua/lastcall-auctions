using LastCallMotorAuctions.API.Models;
using LastCallMotorAuctions.API.DTOs;

namespace LastCallMotorAuctions.API.ViewModels
{
    public class BuyerDashboardViewModel
    {
        public List<Bid> BidList { get; set; } = new List<Bid>();
        public List<AuctionBrowseDto> AuctionList { get; set; } = new List<AuctionBrowseDto>();
        public List<Favourite> Favourites { get; set; } = new List<Favourite>();
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<SellerRatingDto> SellerRatings { get; set; } = new List<SellerRatingDto>();
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = "";
    }

    public class Transaction { public int Id { get; set; } public int AuctionId { get; set; } public decimal Amount { get; set; } public DateTime Timestamp { get; set; } }
    public class SellerRatingDto { public int SellerId { get; set; } public string SellerName { get; set; } = ""; public int Rating { get; set; } public string Comment { get; set; } = ""; }
}

