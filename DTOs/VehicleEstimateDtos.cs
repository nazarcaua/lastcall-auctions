namespace LastCallMotorAuctions.API.DTOs
{
    public class VehicleEstimateRequestDto
    {
        public int Year { get; set; }
        public int MakeId { get; set; }
        public int ModelId { get; set; }
        public int? Mileage { get; set; }
        public string Province { get; set; } = null!;
        public List<IssueInputDto> Issues { get; set; } = new();
    }

    public class IssueInputDto
    {
        public string Category { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Severity { get; set; } = null!;
    }

    public class VehicleEstimateResponseDto
    {
        public string VehicleName { get; set; } = null!;
        public int Year { get; set; }
        public string Make { get; set; } = null!;
        public string Model { get; set; } = null!;
        public decimal EstimatedValue { get; set; }
        public decimal ValueRangeLow { get; set; }
        public decimal ValueRangeHigh { get; set; }
        public decimal BaseValue { get; set; }
        public decimal MileageAdjustment { get; set; }
        public decimal IssuesDeduction { get; set; }
        public decimal TotalRepairCost { get; set; }
        public decimal ValueAfterRepairs { get; set; }
        public string Recommendation { get; set; } = null!;
        public List<RepairEstimateDto> RepairEstimates { get; set; } = new();
    }

    public class RepairEstimateDto
    {
        public string Category { get; set; } = null!;
        public string Issue { get; set; } = null!;
        public string Severity { get; set; } = null!;
        public decimal PartsCost { get; set; }
        public decimal LaborCost { get; set; }
        public decimal TotalCost { get; set; }
        public string RepairTimeEstimate { get; set; } = null!;
    }
}
