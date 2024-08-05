using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Driver
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string AccountUsername { get; set; }

    }
}
