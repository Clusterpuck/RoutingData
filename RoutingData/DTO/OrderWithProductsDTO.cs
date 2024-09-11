using RoutingData.Models;

namespace RoutingData.DTO
{
    public class OrderWithProductsDTO
    {
        public OrderInDTO Order { get; set; }
        public List<OrderProduct> Products { get; set; }
    }
}
