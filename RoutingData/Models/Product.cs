using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string UnitOfMeasure {  get; set; }
    }
}
