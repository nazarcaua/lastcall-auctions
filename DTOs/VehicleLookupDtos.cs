namespace LastCallMotorAuctions.API.DTOs
{
    public class VehicleYearDto
    {
        public short Year { get; set; }
    }

    public class VehicleMakeDto
    {
        public int MakeId { get; set; }
        public string Name { get; set; } = null!;
    }

    public class VehicleModelDto
    {
        public int ModelId { get; set; }
        public string Name { get; set; } = null!;
    }
}
