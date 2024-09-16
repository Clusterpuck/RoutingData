using Microsoft.EntityFrameworkCore;
using RoutingData.Models;

namespace RoutingData.DTO
{
    /// <summary>
    /// <class>DictionareOrderDetails</class>
    /// A class shared by Orders and Delivery controllers for giving order details 
    /// </summary>
    public class DictionaryOrderDetails
    {
        public Dictionary<int, OrderDetailsDTO> OrderDetailsDict { get; set; } = new Dictionary<int, OrderDetailsDTO>();

        /// <summary>
        /// Method <c>GetOrderDetails</c>
        /// Populates a Dictionary of OrderDetailDTOs, pulled only from items that have an active status
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<int, OrderDetailsDTO>> GetOrderDetails( ApplicationDbContext _context)
        {
            var orderDetails = await _context.Orders
            // .Where(order => order.Status == Order.ORDER_STATUSES[0])
                .Join(_context.Locations.
                    Where(location => location.Status == RoutingData.Models.Location.LOCATION_STATUSES[0]),
                    order => order.LocationId,
                    location => location.Id,
                    (order, location) => new { order, location })
                .Join(_context.Customers.
                    Where(customer => customer.Status == Customer.CUSTOMER_STATUSES[0]),
                    combined => combined.order.CustomerId,
                    customer => customer.Id,
                    (combined, customer) => new { combined.order, combined.location, customer })
                .Join(_context.OrderProducts,
                    combined => combined.order.Id,
                    orderProduct => orderProduct.OrderId,
                    (combined, orderProduct) => new { combined.order, combined.location, combined.customer, orderProduct })
                .Join(_context.Products.
                    Where(product => product.Status == Product.PRODUCT_STATUSES[0]),
                    combined => combined.orderProduct.ProductId,
                    product => product.Id,
                    (combined, product) => new { combined.order, combined.location, combined.customer, product })
                .ToListAsync();

            OrderDetailsDict = orderDetails
                .GroupBy(g => new { g.order, g.location, g.customer })
                .Select(g => new OrderDetailsDTO
                {
                    OrderID = g.Key.order.Id,
                    OrderNotes = g.Key.order.OrderNotes,
                    DateOrdered = g.Key.order.DateOrdered,
                    Status = g.Key.order.Status,
                    DeliveryDate = g.Key.order.DeliveryDate,
                    Address = g.Key.location.Address,
                    Latitude = g.Key.location.Latitude,
                    Longitude = g.Key.location.Longitude,
                    CustomerName = g.Key.customer.Name,
                    CustomerPhone = g.Key.customer.Phone,
                    Position = g.Key.order.PositionNumber,
                    Delayed = g.Key.order.Delayed, //amira added
                    ProductNames = g.Select(x => x.product.Name)
                    .ToList(),
                })
            // Returning the result as a dictionary where the key is OrderID and the value is OrderDetailsDTO
                .ToDictionary(x => x.OrderID);

            return OrderDetailsDict;
        }

    }
}
