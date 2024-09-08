using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Address { get; set; }
        public string Suburb { get; set; }
        public string State { get; set; }
        public int PostCode { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

    }
}
