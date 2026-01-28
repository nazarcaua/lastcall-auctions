namespace LastCallMotorAuctions.API.DTOs
{
    public class VehicleResponseDto
    {
        public int MakeId { get; set; }
        public string Make { get; set; } = null!;
        public int ModelId { get; set; }
        public string Model { get; set; } = null!;
        public short Year { get; set; }
    }
}
