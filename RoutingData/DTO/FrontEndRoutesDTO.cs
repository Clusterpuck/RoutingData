using System.Text;

namespace RoutingData.DTO
{

    //Object structure that front end will send to C#
    public class RouteRequest
    {
        public int NumVehicle { get; set; }
        public string calcType { get; set; }
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
        public List<int> Orders { get; set; }
    }



    //Object structure that frontend expects back from a route calc
    public class CalcRouteOutput
    {
        // has vehicle id and list of OrderDetails
        public int VehicleId { get; set; }
        public List<OrderDetailsDTO> Orders { get; set; }

        public CalcRouteOutput() {
            Orders = new List<OrderDetailsDTO>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"VehicleId: {VehicleId}");
            sb.AppendLine("Orders:");
            foreach (var order in Orders)
            {
                sb.AppendLine(order.ToString());
                sb.AppendLine(new string('-', 20)); // Separator between orders
            }
            return sb.ToString();
        }
    }

    public class OrderStatusDTO
    {
        public string Username { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; }
    }
}
