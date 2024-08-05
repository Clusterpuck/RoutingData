using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }
        public string LicensePlate { get; set; }
    }
}
