using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    //comment for change
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public int CustomerId { get; set; }
        public int LocationId { get; set; }
        public int DeliveryRouteId { get; set; }
        public int PositionNumber { get; set; }
        public string Status { get; set; }
        public DateTime DeliveryDate {  get; set; } = DateTime.Today;

    }
}
