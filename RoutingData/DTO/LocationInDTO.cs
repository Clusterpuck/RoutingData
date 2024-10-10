namespace RoutingData.DTO
{
    public class LocationInDTO
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Suburb { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int PostCode { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
