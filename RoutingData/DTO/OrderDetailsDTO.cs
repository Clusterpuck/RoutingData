namespace RoutingData.DTO
{
    public class OrderDetailsDto
    {
        public int OrderID { get; set; }
        public string OrderNotes { get; set; }
        public DateTime DateOrdered { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public List<string> ProductNames { get; set; }
    }
}
