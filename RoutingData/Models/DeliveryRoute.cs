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
        public DateTime DeliveryDate {  get; set; }
#if OFFLINE_DATA
        public int VehicleId { get; set; }
#else
        public String VehicleLicense { get; set; }
#endif
        public String DriverUsername { get; set; }
        public int DepotID { get; set; } //references a location object
    }
}
