namespace LastCallMotorAuctions.API.Models
{
    public class VehicleMake
    {
        public int MakeId { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<VehicleModel> Models { get; set; } = new List<VehicleModel>();
        public ICollection<VehicleYearMake> YearMakes { get; set; } = new List<VehicleYearMake>();
    }
}
