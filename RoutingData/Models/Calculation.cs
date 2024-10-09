using RoutingData.DTO;
using System.ComponentModel.DataAnnotations;

namespace RoutingData.Models
{
    public class Calculation
    {
        public static readonly String[] CALCULATION_STATUS = { "COMPLETED", "CALCULATING", "FAILED" };

        [Key]
        public String ID {get; set;} = Guid.NewGuid().ToString();
        public String Status { get; set; } = CALCULATION_STATUS[1]; //each new calc should always start as calculating
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; }
        public DateTime DeliveryDate { get; set; }
        public String ErrorMessage { get; set; } = "";
        public int NumOfOrders { get; set; }
        public int MaxVehicles { get; set; }
        public Boolean UsedQuantum { get; set; }
        public Boolean UsedMapBox { get; set; }
        public Boolean UsedXMeans { get; set; }

    }
}
