namespace RoutingData.DTO
{
    public class OrderInDTO
    {
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public string CustomerName { get; set; }
        public int LocationId { get; set; }
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
    }

    public class UpdateOrderStatusDTO
    { 
        public int OrderId { get; set; }
        public string Status { get; set; }
    }

    public class EditOrderDTO
    { 
        public int OrderId { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public int LocationId { get; set; }
        public DateTime DeliveryDate { get;set; }
        public string OrderNotes { get; set; }
        public List<OrderProductInDTO> Products { get; set; }
    }
}
