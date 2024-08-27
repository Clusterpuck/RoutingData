using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Driver
    {
        [Key]
        public string Username { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }

    }
}
