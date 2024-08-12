using RoutingData.Models;
using System.Collections;

namespace RoutingData.DTO
{
    public class CalculatedRoutesDTO
    {
        public List<List<Order>> RoutesList { get; set; }
    }
}
