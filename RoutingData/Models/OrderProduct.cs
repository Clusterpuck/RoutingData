using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models
{
    [PrimaryKey(nameof(OrderId), nameof(ProductId))]
    public class OrderProduct
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
    }
}
