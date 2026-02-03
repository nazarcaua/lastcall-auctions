namespace LastCallMotorAuctions.API.Models
{
    public class VehicleYear
    {
        public short Year { get; set; }

        public ICollection<VehicleYearMake> YearMakes { get; set; } = new List<VehicleYearMake>();
    }
}
