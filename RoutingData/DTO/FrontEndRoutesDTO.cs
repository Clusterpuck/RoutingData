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
        public List<OrderDetail> Orders { get; set; }

        public CalcRouteOutput() {
            Orders = new List<OrderDetail>();
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

    public class OrderDetail
    {
        public int OrderId { get; set; }
        public string Addr { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public int Position { get; set; }
        public List<string> ProdNames { get; set; }
        public string Notes { get; set; }
        public DateTime DeliveryDate { get; set; }
        


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"OrderId: {OrderId}");
            sb.AppendLine($"Addr: {Addr}");
            sb.AppendLine($"Lat: {Lat}");
            sb.AppendLine($"Lon: {Lon}");
            sb.AppendLine($"Status: {Status}");
            sb.AppendLine($"CustomerName: {CustomerName}");
            sb.AppendLine($"Phone: {Phone}");
            sb.Append("ProdNames: ");
            sb.AppendLine(ProdNames != null ? string.Join(", ", ProdNames) : "None");
            return sb.ToString();
        }
    }

}
