namespace LastCallMotorAuctions.API.Models
{
    public class UserStatus { public byte StatusId { get; set; } public string Name { get; set; } = null!; }
    public class ListingStatus { public byte StatusId { get; set; } public string Name { get; set; } = null!; }
    public class AuctionStatus { public byte StatusId { get; set; } public string Name { get; set; } = null!; }
    public class PaymentStatus { public byte StatusId { get; set; } public string Name { get; set; } = null!; }
    public class Currency { public string CurrencyCode { get; set; } = null!; public string Name { get; set; } = null!; }
    public class PaymentMethod { public int MethodId { get; set; } public string Name { get; set; } = null!; }
    public class NotificationType { public short TypeId { get; set; } public string Name { get; set; } = null!; }
}
