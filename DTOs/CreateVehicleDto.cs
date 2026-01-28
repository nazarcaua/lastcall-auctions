namespace LastCallMotorAuctions.API.DTOs
{
    public class CreateVehicleDto
    {
        public string Make { get; set; } = null!;
        public string Model { get; set; } = null!;
        public short Year { get; set; }
    }
}
