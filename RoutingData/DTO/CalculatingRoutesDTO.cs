using RoutingData.Models;
using System.Collections;

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
        public int order_ID { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }


    //Response from python calculation, a list of list of orders
    //Grouped by vehicle then ordered by order id
    public class RouteRequestListDTO
    {
        public List<List<int>> orders { get; set; }
    }
}
