﻿using Microsoft.CodeAnalysis;
using System.Text;

namespace RoutingData.DTO
{
    /// <summary>
    ///Object structure that front end will send to C#
    ///<para>int NumVehicle, 
    ///string calcType, 
    ///DateTime DeliveryDate
    ///List int Orders
    ///</para> 
    /// </summary>
    public class RouteRequest
    {
        public int NumVehicle { get; set; }
        public string CalcType { get; set; } = "brute";
        public string Distance { get; set; } = "cartesian";
        public string Type { get; set; } = "xmeans";

        public int Depot { get; set; } //should represent a location id that has field depot set to true
        public DateTime DeliveryDate { get; set; } = DateTime.Today;
        public List<int> Orders { get; set; }
    }


    /// <summary>
    /// Object structure that frontend expects back from a route calc
    /// 
    /// </summary>
    public class CalcRouteOutput
    {
        // has vehicle id and list of OrderDetails
#if OFFLINE_DATA
        public int VehicleId { get; set; }
#else
        public String VehicleId { get; set; }
#endif
        public int DeliveryRouteID { get; set; }
        public String DriverUsername { get; set; }
        public DateTime DeliveryDate { get; set; }
        public List<OrderDetailsDTO> Orders { get; set; }
        public RoutingData.Models.Location Depot {  get; set; }

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

    // amira added
    public class OrderDelayedDTO
    {
        public string Username { get; set; }
        public int OrderId { get; set; }
        public string Delayed { get; set; }
    }

    public class OrderIssueDTO
    { 
        public string Username { get; set; }
        public int OrderId { get; set; }
        public string DriverNote { get; set; }
    }

    // used when updating the route
    public class UpdateRouteDTO
    {
        public int routeID { get; set; }
        public string driverUsername { get; set; }
        public string vehicleID { get; set; }
    }
}
