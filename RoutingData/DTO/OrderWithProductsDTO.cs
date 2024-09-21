using RoutingData.Models;

namespace RoutingData.DTO
{
    public class OrderWithProductsDTO
    {
        public OrderInDTO Order { get; set; }
        public List<OrderProductInDTO> Products { get; set; }
    }

    public class ProductGetOrderDTO // used in get orders
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string UnitOfMeasure { get; set; }
    }

    public class OrderDetailsWithProductsDTO
    {
        public int OrderID { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int Position { get; set; } 
        public List<ProductGetOrderDTO> Products { get; set; } 
        public string OrderNotes { get; set; }
        public DateTime DateOrdered { get; set; }
        public DateTime DeliveryDate { get; set; }
        public bool Delayed { get; set; }
    }
}
