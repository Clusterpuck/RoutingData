using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    //comment for change
    public class Order
    {
        public static readonly String[] ORDER_STATUSES = { "PLANNED", "ON-ROUTE", "DELAYED", "DELIVERED", "CANCELLED", "ASSIGNED" };

        [Key]
        public int Id { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public int CustomerId { get; set; }
        public int LocationId { get; set; }
        public int DeliveryRouteId { get; set; }
        public int PositionNumber { get; set; }
        public string? Status { get; set; }
        public DateTime DeliveryDate {  get; set; } = DateTime.Today;

    }
}
