using RoutingData.Models;

namespace RoutingData.DTO
{
    public class OrderWithProductsDTO
    {
        public OrderInDTO Order { get; set; }
        public List<OrderProductInDTO> Products { get; set; }
    }
}
