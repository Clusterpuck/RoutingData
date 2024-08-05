using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class DriverAccount
    {
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
