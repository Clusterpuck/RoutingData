using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Account
    {
        public static readonly String[] ACCOUNT_STATUSES = { "Active", "Inactive" };
        public static readonly String[] ACCOUNT_ROLES = { "Driver", "Admin" };
        public static readonly int PASSWORD_LENGTH = 6;


        [Key]
        public string Username { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
    }
}
