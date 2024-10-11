namespace RoutingData.DTO
{
    public class VehicleInDTO
    {
        public string LicensePlate { get; set; }

        // amelie added
        public string Make { get; set; }
        public string Model { get; set; }
        public string Colour { get; set; }
        public int Capacity { get; set; } // adding capacity even though we wont add logic for capacity checks, it makes sense that vehicle includes it
    }
}
