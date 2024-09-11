using Humanizer;
using RoutingData.Models;
using System.Text;

namespace RoutingData.DTO
{
    public class OrderDetailsDTO
    {
        public int OrderID { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int Position { get; set; }
        public List<string> ProductNames { get; set; }
        public string OrderNotes { get; set; }
        public DateTime DateOrdered { get; set; }
        public DateTime DeliveryDate {  get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"OrderId: {OrderID}");
            sb.AppendLine($"Addr: {Address}");
            sb.AppendLine($"Lat: {Latitude}");
            sb.AppendLine($"Lon: {Longitude}");
            sb.AppendLine($"Status: {Status}");
            sb.AppendLine($"CustomerName: {CustomerName}");
            sb.AppendLine($"Phone: {CustomerPhone}");
            sb.Append("ProdNames: ");
            sb.AppendLine(ProductNames != null ? string.Join(", ", ProductNames) : "None");
            return sb.ToString();
        }

    }
}
