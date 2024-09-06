using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Account
    {
        [Key]
        public string Username { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Address { get; set; }
    }
}
