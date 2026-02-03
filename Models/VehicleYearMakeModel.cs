namespace LastCallMotorAuctions.API.Models
{
    /// <summary>
    /// Junction table representing which models are available for a given year+make combination.
    /// </summary>
    public class VehicleYearMakeModel
    {
        public int YearMakeModelId { get; set; }
        public int YearMakeId { get; set; }
        public int ModelId { get; set; }

        public VehicleYearMake? YearMake { get; set; }
        public VehicleModel? Model { get; set; }
    }
}
