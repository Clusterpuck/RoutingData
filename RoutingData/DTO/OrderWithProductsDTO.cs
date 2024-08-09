using RoutingData.Models;

namespace RoutingData.DTO
{
    public class OrderWithProductsDTO
    {
        public Order Order { get; set; }
        public List<OrderProduct> Products { get; set; }
    }
}
