using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Status { get; set; }
    }
}
