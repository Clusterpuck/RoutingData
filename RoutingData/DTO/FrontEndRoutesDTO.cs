namespace RoutingData.DTO
{

    //Object structure that front end will send to C#
    public class RouteRequest
    {
        public int NumVehicle { get; set; }
        public List<int> Orders { get; set; }
    }



    //Object structure that frontend expects back from a route calc
    public class CalcRouteOutput
    {
        public int VehicleId { get; set; }
        public List<OrderDetail> Orders { get; set; }
    }

    public class OrderDetail
    {
        public int OrderId { get; set; }
        public string Addr { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public string Status { get; set; }
        public List<string> ProdNames { get; set; }
    }

}
