using RoutingData.Models;
using System.Collections;
using System.Text;

namespace RoutingData.DTO
{
    //The object that python accepts
    public class CalculatingRoutesDTO
    {
        public int num_vehicle {  get; set; }
        public List<OrderInRouteDTO> orders { get; set; }
    }

    //Populated objects in the list to be sent to order calculator
    public class OrderInRouteDTO
    {
        public int order_id { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }


    //Response from python calculation, a list of list of orders
    //Grouped by vehicle then ordered by order id
    public class RouteRequestListDTO : List<List<int>>
    {
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RouteRequestListDTO:");
            foreach (var route in this)
            {
                sb.Append("[");
                sb.Append(string.Join(", ", route));
                sb.AppendLine("]");
            }
            return sb.ToString();
        }
    }

}
