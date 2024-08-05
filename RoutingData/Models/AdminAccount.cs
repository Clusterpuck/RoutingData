using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class AdminAccount
    {
        [Key]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
