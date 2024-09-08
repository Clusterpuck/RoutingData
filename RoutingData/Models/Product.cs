using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace RoutingData.Models
{
    public class Product
    {
        public static readonly String[] PRODUCT_STATUSES = { "Active", "Inactive" };

        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string UnitOfMeasure {  get; set; }
        public string Status { get; set; }
    }
}
