using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Customer
    {
        public static readonly String[] CUSTOMER_STATUSES = { "Active", "Inactive" };
        [Key]
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
    }
}
