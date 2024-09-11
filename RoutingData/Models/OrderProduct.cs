using Microsoft.EntityFrameworkCore;

namespace RoutingData.Models
{
    [PrimaryKey(nameof(OrderId), nameof(ProductId))]
    public class OrderProduct
    {
        public static readonly String[] ORDERPRODUCTS_STATUSES = { "Active", "Inactive" };
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
    }
}
