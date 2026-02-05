using System.ComponentModel.DataAnnotations;

namespace LastCallMotorAuctions.API.DTOs
{
    public class UpdateListingDto
    {
        [StringLength(120, MinimumLength = 1)]
        public string? Title { get; set; }
        public string? Description { get; set; }

        [Range(1886, 2100)]
        public short? Year { get; set; }

        public int? MakeId { get; set; }
        public int? ModelId { get; set; }

        [StringLength(17)]
        public string? Vin { get; set; }

        [Range(0, int.MaxValue)]
        public int? Mileage { get; set; }

        [Range(1, 5)]
        public byte? ConditionGrade { get; set; }

        [StringLength(80)]
        public string? City { get; set; }

        [StringLength(80)]
        public string? Region { get; set; }

        [StringLength(80)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        // 1 = Draft, 2 = Active, 3 = Archived
        [Range(1, 3)]
        public byte? StatusId { get; set; }
    }
}
