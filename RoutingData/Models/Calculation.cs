using RoutingData.DTO;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoutingData.Models
{
    public class Calculation
    {
        public static readonly String[] CALCULATION_STATUS = { "COMPLETED", "CALCULATING", "FAILED" };

        [Key]
        public String ID {get; set;} = Guid.NewGuid().ToString();

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CalculationNumber { get; set; } // Auto-incrementing integer field
        public String Status { get; set; } = CALCULATION_STATUS[1]; //each new calc should always start as calculating

        [Column(TypeName = "TEXT")] //needed to store the long string
        public String PythonPayload { get; set; } = "";
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; }
        public DateTime DeliveryDate { get; set; }
        public String ErrorMessage { get; set; } = "";
        public int NumOfOrders { get; set; }
        public int MaxVehicles { get; set; }
        public Boolean UsedQuantum { get; set; }
        public Boolean UsedMapBox { get; set; }
        public Boolean UsedXMeans { get; set; }
       // public int DepotID { get; set; }

    }
}
