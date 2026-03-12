using System.ComponentModel.DataAnnotations;

namespace LastCallMotorAuctions.API.DTOs
{
    public class VehicleItemDto
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [Required]
        [Range(1886, 2100)]
        public short Year { get; set; }

        [Required]
        public int MakeId { get; set; }

        [Required]
        public int ModelId { get; set; }

        public string? Vin { get; set; }
        public int? Mileage { get; set; }
        [Required]
        [Range(1, 5)]
        public byte ConditionGrade { get; set; }

        public string? Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal StartPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReservePrice { get; set; }
    }

    public class CreateListingDto
    {
        // Optional group title - if provided, multiple vehicles will be part of the same auction group
        public string? AuctionGroupTitle { get; set; }

        [Required]
        public List<VehicleItemDto> Vehicles { get; set; } = new();

        // Group-level auction start time (shared) - if null, starts immediately
        public DateTime? StartTime { get; set; }

        // Group-level auction end time (shared)
        public DateTime? EndTime { get; set; }
    }
}
