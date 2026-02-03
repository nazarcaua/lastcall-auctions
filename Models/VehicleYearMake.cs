namespace LastCallMotorAuctions.API.Models
{
    /// <summary>
    /// Junction table representing which makes are available for a given year.
    /// </summary>
    public class VehicleYearMake
    {
        public int YearMakeId { get; set; }
        public short Year { get; set; }
        public int MakeId { get; set; }

        public VehicleYear? VehicleYear { get; set; }
        public VehicleMake? Make { get; set; }

        public ICollection<VehicleYearMakeModel> YearMakeModels { get; set; } = new List<VehicleYearMakeModel>();
    }
}
