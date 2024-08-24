using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
   //removed to test build fail on merge
   //public class Order
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateOrdered { get; set; }
        public string OrderNotes { get; set; }
        public int CustomerId { get; set; }
        public int LocationId { get; set; }
        public int DeliveryRouteId { get; set; }
        public int PositionNumber { get; set; }

    }
}
