namespace RoutingData.DTO
{
    public class OrderInDTO
    {
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public int CustomerId { get; set; }
        public int LocationId { get; set; }
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
    }
}
