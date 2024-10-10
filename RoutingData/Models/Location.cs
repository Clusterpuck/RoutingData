using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    //[PrimaryKey(nameof(Longitude), nameof(Latitude), nameof(CustomerName))]
    public class Location
    {
        public static readonly String[] LOCATION_STATUSES = { "Active", "Inactive" };
        [Key]
        public int Id { get; set; }//id kept for easy access and reference

        //Composite key still used to keep unique
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Suburb { get; set; }
        public string State { get; set; }
        public int PostCode { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public Boolean IsDepot { get; set; } = false;

    }
}
