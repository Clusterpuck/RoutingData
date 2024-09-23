namespace RoutingData.DTO
{
    using RoutingData.Models;

    /// <summary>
    /// Return values to send collection of summary data used by home page
    /// </summary>
    public class HomeData
    {
        //the total number of orders on theat date
        public int OrdersCount { get; set; }
        //number of orders today that have delayed status
        public int ActiveOrdersCount { get; set; }
        public int DelaysCount { get; set; }

        //any orders that have been assigned issue status
        public List<Order> OrdersWithIssues { get; set; }
        //number of routes assigned to today
        public int RoutesCount { get; set; }
        //list of drivers that have routes today
        public List<string> DriversOnRoutes { get; set; }

    }
}
