using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using RoutingData.DTO;
using RoutingData.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NuGet.Packaging.PackagingConstants;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : Controller
    {

        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GetHomeData populates basic statistic for landing page
        /// All based on orders and routes polanned for Today
        /// Today determined by DateTime.Now
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<HomeData>> GetHomeData()
        {//Uses orders and routes and accounts data. Must first confirm these are all present
            if( _context.Orders == null || _context.DeliveryRoutes == null  || _context.Accounts == null )
            {
                return NotFound();
            }
            
            //all order details for today is managed. Data set should be small once filtered to day
            //so getting list of just today is enough. Saves too many DB requests
            DateTime today = DateTime.Now;
            List<Order> todayOrders = await _context.Orders.
                Where(order => order.DeliveryDate.Date == today.Date).
                ToListAsync();  
            List<Order> issueOrders = todayOrders.
                Where( order => order.Status == Order.ORDER_STATUSES[5]).
                ToList();//filters to issue status only
            int numOrders = todayOrders.Count;
            int delayedCount = todayOrders.Where(order => (order.Delayed)).Count(); //counts orders with delayed status
            int deliveredCount = todayOrders.Where(order => (order.Status == Order.ORDER_STATUSES[2])).Count();
            //on route orders include orders that have any issues
            int ordersOnRouteCount = todayOrders.
                Where(order => (order.Status == Order.ORDER_STATUSES[1])).Count() + issueOrders.Count;

            List<DeliveryRoute> routesToday = await _context.DeliveryRoutes.
                Where(route => (route.DeliveryDate.Date == today.Date)).
                ToListAsync();
            int routeCount = routesToday.Count;

            var ordersByRouteId = todayOrders
                .GroupBy(order => order.DeliveryRouteId)
                .ToDictionary(g => g.Key, g => g.ToList());

            int activeRouteCount = 0;
            int plannedRouteCount = 0;
            int finishedRouteCount = 0;

            foreach (var route in routesToday)
            {
                if (ordersByRouteId.TryGetValue(route.Id, out var ordersInRoute))
                {
                    // Reference the status constants using the indices from ORDER_STATUSES array
                    bool hasActiveOrder = ordersInRoute.Any(order => order.Status == Order.ORDER_STATUSES[1]); // "ON-ROUTE"
                    bool hasPlannedOrder = ordersInRoute.Any(order => order.Status == Order.ORDER_STATUSES[4]); // "ASSIGNED"
                    bool areAllFinished = ordersInRoute.All(order =>
                        order.Status == Order.ORDER_STATUSES[2] ||  // "DELIVERED"
                        order.Status == Order.ORDER_STATUSES[5] ||  // "ISSUE"
                        order.Status == Order.ORDER_STATUSES[3]);   // "CANCELLED"

                    if (hasActiveOrder) activeRouteCount++;
                    if (hasPlannedOrder) plannedRouteCount++;
                    if (areAllFinished) finishedRouteCount++;
                }
            }

            List<string> drivers = routesToday.Select(route => route.DriverUsername).ToList();
            HomeData homeData = new HomeData()
            {
                DriversOnRoutes = drivers,
                OrdersCount = numOrders,
                ActiveOrdersCount = ordersOnRouteCount,
                DeliveredCount = deliveredCount,
                DelaysCount = delayedCount,
                OrdersWithIssues = issueOrders,
                RoutesCount = routeCount,
                ActiveRouteCount = activeRouteCount,
                PlannedRouteCount = plannedRouteCount,
                FinishedRouteCount = finishedRouteCount,
            };

            return homeData;

        }
    }
}
