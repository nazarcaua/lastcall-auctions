using System.ComponentModel.DataAnnotations;

namespace LastCallMotorAuctions.API.DTOs
{
    public class CreateListingDto
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        [Range(1886, 2100)]
        public short Year { get; set; }

        [Required]
        public int MakeId { get; set; }

        [Required]
        public int ModelId { get; set; }

        [StringLength(17)]
        public string? Vin { get; set; }

        [Range(0, int.MaxValue)]
        public int? Mileage { get; set; }

        [Required]
        [Range(1, 5)]
        public byte ConditionGrade { get; set; }

        [Required]
        [StringLength(80)]
        public string City { get; set; } = null!;

        [StringLength(80)]
        public string? Region { get; set; }

        [Required]
        [StringLength(80)]
        public string Country { get; set; } = null!;

        [StringLength(20)]
        public string? PostalCode { get; set; }

        // Auction fields (optional) - if provided an auction will be created for this listing
        [Range(0, double.MaxValue)]
        public decimal? StartPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ReservePrice { get; set; }

        // End time for the auction in UTC expected from client or local datetime converted by server
        public DateTime? EndTime { get; set; }
    }
}
