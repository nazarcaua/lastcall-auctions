namespace LastCallMotorAuctions.API.Models
{
    public class VehicleModel
    {
        public int ModelId { get; set; }
        public int MakeId { get; set; }
        public string Name { get; set; } = null!;

        public VehicleMake? Make { get; set; }
    }
}
