using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Vehicle
    {
        [Key]
        public string LicensePlate { get; set; }
        public string Status { get; set; }
    }
}
