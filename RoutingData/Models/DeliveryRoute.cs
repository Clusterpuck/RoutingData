using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class DeliveryRoute
    {
        [Key]
        public int Id { get; set; }
        public double Distance { get; set; }
        public int EstimatedTimeMinutes { get; set; }
        public int CreatorAdminId { get; set; }
        public DateTime TimeCreated { get; set; }
        public int VehicleId { get; set; }
        public int DriverId { get; set; }
    }
}
