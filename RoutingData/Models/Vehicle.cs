using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Vehicle
    {
        public static readonly String[] VEHICLE_STATUSES = { "Active", "Inactive" };
        [Key]
#if OFFLINE_DATA
        public int Id { get; set; }
#else
#endif
        //TODO Add more relevent fields like Make, Model, Capacity etc
        public string LicensePlate { get; set; }
        public string Status { get; set; }
    }
}
