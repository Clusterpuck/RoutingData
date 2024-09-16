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
using Newtonsoft.Json;
using RoutingData.DTO;
using RoutingData.Models;
using static NuGet.Packaging.PackagingConstants;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class DeliveryRoutesController : ControllerBase
    {


#if OFFLINE_DATA

        private readonly OfflineDatabase _offlineDatabase;

        public DeliveryRoutesController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }


        [HttpPost("update-status")]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderStatus(OrderStatusDTO orderStatusDTO)
        {//given, username, orderID and new status. 
         //Change the status of the order, then return the updated deliveryroute for that driver.

            //first check orderID is in the correct delivery route
            DeliveryRoute driverRoute = _offlineDatabase.deliveryRoutes
                                               .FirstOrDefault(r => r.DriverUsername == orderStatusDTO.Username);
            if (driverRoute == null)
            {
                return NotFound("No route assigned to provided driver");
            }
            Order order = _offlineDatabase.Orders.FirstOrDefault( order => order.Id == orderStatusDTO.OrderId );
            if (order == null)
            {
                return NotFound(" No matching order in that drivers route ");
            }
            //Confirmed both IDs accurate at this point

            //updating order to requested status
            order.Status = orderStatusDTO.Status;

            //Now just get the driversRoute again to return. Which is a CalcRouteOutput
            //Previously created by pythonOutputToFront from an oderDetailsDict and routeRequestListDTO, just a List of List of orderIDs

            //Perhaps better to do deliveryRoute dictionary, get all orders for a delivery route. 
            //But less work to simply do in N time, get all orders matching the delivery route
            List<OrderDetailsDTO> ordersInRoute = new List<OrderDetailsDTO>();
            Dictionary<int, OrderDetailsDTO> orderDetailsDict = _offlineDatabase.MakeOrdersDictionary();
            foreach (Order orderDB in _offlineDatabase.Orders)
            {
                if( orderDB.DeliveryRouteId == driverRoute.Id )
                {
                    ordersInRoute.Add( orderDetailsDict[orderDB.Id] );
                }
            }
            //Now with a list of ordersDetails that belong to the driver route. 
            CalcRouteOutput routeForFrontend = new CalcRouteOutput();
            routeForFrontend.Orders = ordersInRoute;
            routeForFrontend.VehicleId = driverRoute.VehicleId;
            

            return Ok(routeForFrontend);
            
        }

        //converts front end data to the required input for python end point
        private CalculatingRoutesDTO frontDataToPythonData( RouteRequest frontEndData, Dictionary<int, OrderDetailsDTO> orderDetailsDict)
        {
            //From routerequest need to make a list of OrderInRoute
            List<OrderInRouteDTO> routesForPython = new List<OrderInRouteDTO>();

            foreach (int orderID in frontEndData.Orders)
            {
                OrderDetailsDTO orderDetail = orderDetailsDict[orderID];
                OrderInRouteDTO routeDTO = new OrderInRouteDTO();
                routeDTO.lat = orderDetail.Latitude;
                routeDTO.lon = orderDetail.Longitude;
                routeDTO.order_id = orderID;

                routesForPython.Add(routeDTO);
            }

            CalculatingRoutesDTO calcRoute = new CalculatingRoutesDTO();
            calcRoute.orders = routesForPython;

            SubCalcSetting vehicleCluster = new SubCalcSetting();
            vehicleCluster.type = "kmeans";
            vehicleCluster.k = frontEndData.NumVehicle;

            calcRoute.vehicle_cluster_config = vehicleCluster;

            SolverCalcSetting solverCalcSetting = new SolverCalcSetting();
            solverCalcSetting.type = frontEndData.calcType; // "brute";
            solverCalcSetting.distance = "cartesian";
            solverCalcSetting.max_solve_size = 5;

            calcRoute.solver_config = solverCalcSetting;

            return calcRoute;

        }




        // GET: api/DeliveryRoutes
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<DeliveryRoute>>> GetDeliveryRoutes()
        {
            if (_offlineDatabase.deliveryRoutes.Count == 0)
            {
                return NotFound();
            }
            return _offlineDatabase.deliveryRoutes;
        }

        // GET: api/DeliveryRoutes/driver/{driverUsername}
        [HttpGet("driver/{driverUsername}")]
        [Authorize]
        public async Task<ActionResult<CalcRouteOutput>> GetDeliveryRoutesByDriver(string driverUsername)
        {
            DeliveryRoute driverRoute = _offlineDatabase.deliveryRoutes
                                               .FirstOrDefault(r => r.DriverUsername == driverUsername);

            if (driverRoute == null)
            {
                return NotFound($"No delivery routes found for driver with username {driverUsername}");
            }

            CalcRouteOutput calcOutput = deliveryToCalcRouteOutput(driverRoute);

            //Now need to build the CalcRouteOutput and return that object 

            return calcOutput;
        }


        // PUT: api/DeliveryRoutes/Start/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("start/{id}")]
        public async Task<IActionResult> PutDeliveryRoute(int id)
        {
            //Get the delivery route matching the id
            DeliveryRoute startRoute = _offlineDatabase.deliveryRoutes.
                FirstOrDefault(route => route.Id == id);
            if (startRoute == null)
            {
                return NotFound("Delivery route id not in database");
            }
            //Get all the orders in the route, then set their status to on-route
            foreach (Order order in _offlineDatabase.Orders)
            {
                if (order.DeliveryRouteId == id)
                {
                    order.Status = "on-route";
                }
            }

            return Ok();
        }



        private CalcRouteOutput deliveryToCalcRouteOutput( DeliveryRoute deliveryRoute )
        {
            CalcRouteOutput calcRouteOutput = new CalcRouteOutput();
            calcRouteOutput.VehicleId = deliveryRoute.VehicleId;
            //TODO Add conversion

            //dictionary to reference each order to get details
            Dictionary<int, OrderDetailsDTO> orderDetailsDict = _offlineDatabase.MakeOrdersDictionary();
            //This is a dictionary that gets order details from order IDs
            //Now need match orderIDs to the deliveryRoute ID, building a list of order
            //In that route, then converting those to OrderDetails. 

            List<Order> orders = _offlineDatabase.Orders;
            int routeID = deliveryRoute.Id;

            foreach (Order order in orders)
            {
                if( order.DeliveryRouteId == routeID )
                {//Finding the matching orderDetail that belongs to the route
                    //Adding to the calcRouteOutput
                    OrderDetailsDTO orderDetail = orderDetailsDict[order.Id];
                    calcRouteOutput.Orders.Add(orderDetail);
                }

            }
            return calcRouteOutput;

        }


        private void AssignPosAndDelivery(List<CalcRouteOutput> allRoutesCalced, RouteRequest routeRequest)
        {
            //Need to now save these routes to the database.
            //Therefore first assign a DeliveryRoute an autoincrement ID.
            //Then each order in routeRequest is assigned this id. 
            //Make as many new Routes as there are vehicles. Assign in order provided.
            //
            Console.WriteLine("Starting assign pos and delivery");
            Dictionary<int, Order> ordersDict = _offlineDatabase.Orders.ToDictionary(o => o.Id);
            //Clear all previous routes
            _offlineDatabase.deliveryRoutes.Clear();
            List<Account> drivers = _offlineDatabase.Accounts.Where(account => account.Role == "Driver").ToList();
            for (int i = 0; i < routeRequest.NumVehicle; i++)
            {
                DeliveryRoute newRoute = new DeliveryRoute();
                newRoute.Id = _offlineDatabase.deliveryRoutes.Any() ?
                                _offlineDatabase.deliveryRoutes.Last().Id + 1 : 1;

                newRoute.DeliveryDate = DateTime.Today;

                newRoute.VehicleId = _offlineDatabase.Vehicles[i].Id;
                newRoute.DriverUsername = drivers[i].Username;

                //also need to add position number for each order. 
                //for each orderID in the List of Order Details in the corresponding CalcRouteOutput object in allRoutesCalced
                //need to find the matching order object in offlinedatabase and assign the routeID
                _offlineDatabase.deliveryRoutes.Add(newRoute);
                int pos = 1;
                foreach (OrderDetailsDTO order in allRoutesCalced[i].Orders)
                {
                    int orderID = order.OrderID;
                    Order dbOrder = ordersDict[orderID];
                    dbOrder.DeliveryRouteId = newRoute.Id;
                    dbOrder.PositionNumber = pos;
                    pos++;
                }

            }

        }

        
        private Task CheckRouteMax(RouteRequest routeRequest)
        {
            // Find the minimum of the driver count and vehicle count
            int maxVehicles = Math.Min(
                        _offlineDatabase.Accounts.Where(acc => acc.Role == "Driver").Count(),
                        _offlineDatabase.Vehicles.Count()
);

            // Only assign the minimum value if the current NumVehicle exceeds it
            if (routeRequest.NumVehicle > maxVehicles)
            {
                routeRequest.NumVehicle = maxVehicles;
            }
            return Task.CompletedTask;
        }


        



#else
        private readonly ApplicationDbContext _context;



        public DeliveryRoutesController(ApplicationDbContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Method <c>UpdateOrderStatus</c> 
        /// </summary>
        /// <param name="orderStatusDTO"></param>
        /// <returns></returns>
        [HttpPost("update-status")]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderStatus(OrderStatusDTO orderStatusDTO)
        {
            //Check if a valid status provided before using ay database requests
            if( !Order.ORDER_STATUSES.Contains(orderStatusDTO.Status) )
            {
                string availableStatuses = string.Join(", ", Order.ORDER_STATUSES);
                return BadRequest($"Invalid status sent of {orderStatusDTO.Status} +" +
                    $" Must be either one of: {availableStatuses}");
            }

            // check if the delivery route exists for the driver
            var driverRoute = await _context.DeliveryRoutes
                                    .FirstOrDefaultAsync(r => r.DriverUsername == orderStatusDTO.Username);

            if (driverRoute == null)
            {
                return NotFound("No route assigned to provided driver");
            }

            // check if the order exists and is part of the driver's delivery route
            var order = await _context.Orders
                              .FirstOrDefaultAsync(o => o.Id == orderStatusDTO.OrderId && 
                              o.DeliveryRouteId == driverRoute.Id);

            if (order == null)
            {
                return NotFound("No matching order in that driver's route");
            }

            // update the order status
            order.Status = orderStatusDTO.Status;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            //each status update request should return the relevent route it was updated for
            // retrieve all orders for the delivery route
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails(_context);
            CalcRouteOutput routeForFrontend = await DeliveryToCalcRouteOutput(driverRoute, dictionaryOrderDetails.OrderDetailsDict);

            /*var ordersInRoute = await _context.Orders
                                        .Join(_context.Locations,
                                                order => order.LocationId,
                                                location => location.Id,
                                                (order, location) => new { order, location })
                                          .Where(joined => joined.order.DeliveryRouteId == driverRoute.Id)
                                          .Select(joined => new OrderDetailsDTO
                                          {
                                              OrderID = joined.order.Id,
                                              Latitude = joined.location.Latitude,
                                              Longitude = joined.location.Longitude,
                                              Status = joined.order.Status,
                                          })
                                          .ToListAsync();

            // prepare the CalcRouteOutput to return
            CalcRouteOutput routeForFrontend = new CalcRouteOutput
            {
                Orders = ordersInRoute,
                VehicleId = driverRoute.VehicleLicense
            };*/

            return Ok(routeForFrontend);
        }

        // amira added
        /// <summary>
        /// Method <c>UpdateOrderDelayed</c> 
        /// </summary>
        /// <param name="orderDelayedDTO"></param>
        /// <returns></returns>
        [HttpPost("update-delayed")]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderDelayed(OrderDelayedDTO orderDelayedDTO)
        {
            //Check if a valid boolean provided before using ay database requests
            if ( !( orderDelayedDTO.Delayed.Equals("true") || orderDelayedDTO.Delayed.Equals("false") ) )
            {
                return BadRequest($"Invalid Delayed value sent of {orderDelayedDTO.Delayed} +" +
                    " Must be either: true or false.");
            }

            // check if the delivery route exists for the driver
            var driverRoute = await _context.DeliveryRoutes
                                    .FirstOrDefaultAsync(r => r.DriverUsername == orderDelayedDTO.Username);

            if (driverRoute == null)
            {
                return NotFound("No route assigned to provided driver");
            }

            // check if the order exists and is part of the driver's delivery route
            var order = await _context.Orders
                              .FirstOrDefaultAsync(o => o.Id == orderDelayedDTO.OrderId &&
                              o.DeliveryRouteId == driverRoute.Id);

            if (order == null)
            {
                return NotFound("No matching order in that driver's route");
            }

            // order must be on route to set the delayed status to true
            if (!(order.Status.Equals(Order.ORDER_STATUSES[1])))
            {
                return BadRequest("Order must be on-route to be set as delayed.");
            }

            // update the orders delayed attribute
            order.Delayed = bool.Parse(orderDelayedDTO.Delayed);
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            //each status update request should return the relevent route it was updated for
            // retrieve all orders for the delivery route
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails(_context);
            CalcRouteOutput routeForFrontend = await DeliveryToCalcRouteOutput(driverRoute, dictionaryOrderDetails.OrderDetailsDict);

            return Ok(routeForFrontend);
        }


        /// <summary>
        /// Method <c>FrontDataToPythonAsync</c> 
        /// </summary>
        /// <param name="frontEndData"></param>
        /// <returns></returns>
        // ONLINE VERSION
        //converts front end data to the required input for python end point
        private async Task<CalculatingRoutesDTO> FrontDataToPythonDataAsync(RouteRequest frontEndData)
        {
            // Fetch the order details directly from the database based on the provided order IDs
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails( _context );
           

            // Mapping order details to OrderInRouteDTO
            List<OrderInRouteDTO> routesForPython = new List<OrderInRouteDTO>();

            foreach (int orderID in frontEndData.Orders)
            {
                // Fetch the OrderDetail from the dictionary populated from the SQL query
                OrderDetailsDTO orderDetail = dictionaryOrderDetails.OrderDetailsDict[orderID];
                OrderInRouteDTO routeDTO = new OrderInRouteDTO
                {
                    lat = orderDetail.Latitude,
                    lon = orderDetail.Longitude,
                    order_id = orderID
                };

                routesForPython.Add(routeDTO);
            }

            CalculatingRoutesDTO calcRoute = BuildCalcRoute(frontEndData, routesForPython);

            return calcRoute;
        }


        /// <summary>
        /// Method <c>BuildCalcRoutes</c>
        /// Helper method separated out to factor in changes between using literals and options
        /// Creates an Object used to sent to the Python Quantum request
        /// </summary>
        /// <param name="frontEndData"></param>
        /// <param name="routesForPython"></param>
        /// <returns></returns>
        private CalculatingRoutesDTO BuildCalcRoute(RouteRequest frontEndData, List<OrderInRouteDTO> routesForPython)
        {
            CalculatingRoutesDTO calcRoute = new CalculatingRoutesDTO
            {
                orders = routesForPython,
                vehicle_cluster_config = new SubCalcSetting
                {
                    type = "kmeans",
                    k = frontEndData.NumVehicle
                },
                solver_config = new SolverCalcSetting
                {
                    type = frontEndData.calcType, // "brute";
                    distance = "cartesian",
                    max_solve_size = 5
                }
            };
            return calcRoute;
        }

        /// <summary>
        /// Method <c>PutDeliveryRoute</c>
        /// Using endpoint /start/{id} sets all orders in the route to be On_Route
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // PUT: api/DeliveryRoutes/Start/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("start/{id}")]
        public async Task<IActionResult> PutDeliveryRoute(int id)
        {
            //Get the delivery route matching the id
            DeliveryRoute startRoute = await _context.DeliveryRoutes.
                FirstOrDefaultAsync(route => route.Id == id);
            if (startRoute == null)
            {
                return NotFound("Delivery route id not in database");
            }

            //Get all the active/Planned, not already assigned or on route
            //orders in the route, then set their status to on-route
            // Find all orders with the specified DeliveryRouteId
            var ordersToUpdate = await _context.Orders
                .Where(order => (order.Status == Order.ORDER_STATUSES[4]) && //All orders must be assigned
                                (order.DeliveryRouteId == id) )
                .ToListAsync();

            // Update the Status for all found orders
            foreach (var order in ordersToUpdate)
            {
                order.Status = Order.ORDER_STATUSES[1];//assigned On-Route
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Created("", startRoute);
        }

        /// <summary>
        /// Method <c>GetdeliveryRoutes</c>
        /// returns a list of full detail routes of all routes in the system. 
        /// </summary>
        /// <returns></returns>
        // GET: api/DeliveryRoutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CalcRouteOutput>>> GetDeliveryRoutes()
        {
          if (_context.DeliveryRoutes == null)
          {
              return NotFound();
          }
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails(_context);
            List<CalcRouteOutput> routesDetailed = new List<CalcRouteOutput>();
            List<DeliveryRoute> deliveryRoutes = await _context.DeliveryRoutes.ToListAsync();
            foreach (var route in deliveryRoutes)
            {
                routesDetailed.Add( await DeliveryToCalcRouteOutput(route, dictionaryOrderDetails.OrderDetailsDict));
            }
            return routesDetailed;//await _context.DeliveryRoutes.ToListAsync();
        }


        /// <summary>
        /// Method <c>CalcRouteOutput</c> 
        /// Get the delivery and return as a CalcRouteOutput object which offers more detail
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: api/DeliveryRoutes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CalcRouteOutput>> GetDeliveryRoute(int id)
        {
          if (_context.DeliveryRoutes == null)
          {
              return NotFound();
          }
            var deliveryRoute = await _context.DeliveryRoutes.FindAsync(id);

            if (deliveryRoute == null)
            {
                return NotFound();
            }

            DictionaryOrderDetails dictOrderDetails = new DictionaryOrderDetails();
            await dictOrderDetails.GetOrderDetails(_context);
            CalcRouteOutput routeOutput = await DeliveryToCalcRouteOutput(deliveryRoute, dictOrderDetails.OrderDetailsDict);

            return routeOutput;
        }


        //Most likely this will be changed in future. 
        // PUT: api/DeliveryRoutes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDeliveryRoute(int id, DeliveryRoute deliveryRoute)
        {
            if (id != deliveryRoute.Id)
            {
                return BadRequest();
            }

            _context.Entry(deliveryRoute).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DeliveryRouteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Created("", deliveryRoute);
        }


        /// <summary>
        /// Method <CheckRouteMax</c> 
        /// Confirms the maximum number of drivers hasn't been set
        /// Otherwise changes value to the max
        /// </summary>
        /// <param name="routeRequest"></param>
        /// <returns></returns>
        private async Task CheckRouteMax(RouteRequest routeRequest)
        {
            // Get the count of drivers (Accounts with Role "Driver")
            var driverCount = await _context.Accounts
                .Where(account => ( account.Role == Account.ACCOUNT_ROLES[0] ) && //Only selecting driver role
                    (account.Status == Account.ACCOUNT_STATUSES[0])) //That is active
                .CountAsync();

            // Get the count of vehicles
            var vehicleCount = await _context.Vehicles
                .Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0]) //Only selecting active vehicle
                .CountAsync();

            // Find the minimum of the driver count and vehicle count
            int maxVehicles = Math.Min(driverCount, vehicleCount);

            // Only assign the minimum value if the current NumVehicle exceeds it
            if (routeRequest.NumVehicle > maxVehicles)
            {
                routeRequest.NumVehicle = maxVehicles;
            }
        }


        /// <summary>
        /// Method <c>AssignPosAndDeliveryAsync</c> Assigns vehicle in order available
        /// Need to consider vehicle availability in the future
        /// Date as today and driver in order active
        /// Then position and route id assigned to each order, in order in each list
        /// </summary>
        /// <param name="allRoutesCalced"></param>
        /// <returns></returns>
        private async Task AssignPosAndDeliveryAsync(List<CalcRouteOutput> allRoutesCalced)
        {
            Console.WriteLine("Entering Assign Position and Delivery");
            // Fetch all necessary data from the database

            var ordersDict = await _context.Orders.
                Where(order => order.Status == Order.ORDER_STATUSES[0]).
                ToDictionaryAsync(o => o.Id);
            var drivers = await _context.Accounts.
                Where(account => (account.Role == Account.ACCOUNT_ROLES[0] && //Driver roles only 
                    account.Status == Account.ACCOUNT_STATUSES[0]) ). //Active Drivers only
                ToListAsync();
            var vehicles = await _context.Vehicles.
                Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0]).//Active vehicles only
                ToListAsync();


            for (int i = 0; i < allRoutesCalced.Count; i++)
            {
                // Create and assing values to a new DeliveryRoute object
                var newRoute = new DeliveryRoute
                {
                    DeliveryDate = DateTime.Today,
                    VehicleLicense = vehicles[i].LicensePlate,
                    DriverUsername = drivers[i].Username,
                    TimeCreated = DateTime.Now,
                    //CreatorAdminId = routeRequest.CreatorAdminId // Need to add this field in the RouteRequest
                };

                // Add the new route to the database and save it to generate the ID
                _context.DeliveryRoutes.Add(newRoute);
                await _context.SaveChangesAsync();
                //save route id to return object
                allRoutesCalced[i].DeliveryRouteID = newRoute.Id;


                // Assign position number and DeliveryRouteId for each order
                //assigned based on the returned order from python request
                int pos = 1;
                foreach (var orderDetail in allRoutesCalced[i].Orders)
                {
                    if (ordersDict.TryGetValue(orderDetail.OrderID, out var dbOrder))
                    {
                        dbOrder.DeliveryRouteId = newRoute.Id;  // Assign the newly generated DeliveryRouteId
                        dbOrder.PositionNumber = pos;           // Assign the position number
                        dbOrder.Status = Order.ORDER_STATUSES[4];//Status of order set to assigned
                        pos++;
                    }
                }

                // Save all updated orders in the current route
                await _context.SaveChangesAsync();
            }

            Console.WriteLine("Routes and orders updated successfully.");

        }

        /// <summary>
        /// Method <c>DeleteDeliveryRoute</c> Turns all orders in route back to planned
        /// And set their position and routeIds to -1 before removing the entity from database
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/DeliveryRoutes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliveryRoute(int id)
        {
            if (_context.DeliveryRoutes == null)
            {
                return NotFound();
            }
            var deliveryRoute = await _context.DeliveryRoutes.FindAsync(id);
            if (deliveryRoute == null)
            {
                return NotFound();
            }
            //Need to also set all orders in the route back to planned status, and their deliveryrouteID and position number
            //to -1 to effectively delete
           //First need the list of orders assigned to that route
           List<Order> ordersInRoute = await _context.Orders.
                Where(order => order.DeliveryRouteId == deliveryRoute.Id). //all orders in route
                ToListAsync();
            foreach (var order in ordersInRoute)
            {
                order.Status = Order.ORDER_STATUSES[0]; //changes back to planned
                order.DeliveryRouteId = -1;
                order.PositionNumber = -1;
                order.Delayed = bool.Parse("false"); // change delayed status back to false

                //Marking order as modified
                _context.Orders.Update(order);
            }


            _context.DeliveryRoutes.Remove(deliveryRoute);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Method <c>DeliveryRouteExists</c> helper method to confirm route in database by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool DeliveryRouteExists(int id)
        {
            return (_context.DeliveryRoutes?.Any(e => e.Id == id)).GetValueOrDefault();
        }


        /// <summary>
        /// Method <c>DeliveryToCalcRouteOutput</c>
        /// Using the OrderDetails dictionary, populates a CalcRouteOutput object
        /// For the front end to have the required details
        /// </summary>
        /// <param name="deliveryRoute"></param>
        /// <returns></returns>
        private async Task<CalcRouteOutput> DeliveryToCalcRouteOutput(DeliveryRoute deliveryRoute, Dictionary<int, OrderDetailsDTO> orderDetailsDict)
        {
            CalcRouteOutput calcRouteOutput = new CalcRouteOutput();
            calcRouteOutput.VehicleId = deliveryRoute.VehicleLicense;
            calcRouteOutput.DriverUsername = deliveryRoute.DriverUsername;
            //TODO Add conversion

            //dictionary to reference each order to get details
            //This is a dictionary that gets order details from order IDs
            //Now need match orderIDs to the deliveryRoute ID, building a list of order
            //In that route, then converting those to OrderDetails. 

            List<Order> orders = await _context.Orders.ToListAsync();
            int routeID = deliveryRoute.Id;
            calcRouteOutput.DeliveryRouteID = routeID;

            foreach (Order order in orders)
            {
                if (order.DeliveryRouteId == routeID)
                {//Finding the matching orderDetail that belongs to the route
                    //Adding to the calcRouteOutput
                    OrderDetailsDTO orderDetail = orderDetailsDict[order.Id];
                    calcRouteOutput.Orders.Add(orderDetail);

                }

            }
            return calcRouteOutput;

        }

        bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        // GET: api/DeliveryRoutes/driver/{driverUsername}
        [HttpGet("driver/{driverUsername}")]
        //[Authorize]
        public async Task<ActionResult<CalcRouteOutput>> GetDeliveryRoutesByDriver(string driverUsername)
        {
            if (!IsValidEmail(driverUsername) )
            {
                return BadRequest("Email is not a valid format");
            }

            DeliveryRoute driverRoute = _context.DeliveryRoutes
                                               .FirstOrDefault(r => r.DriverUsername == driverUsername);

            if (driverRoute == null)
            {
                return NotFound($"No delivery routes found for driver with username {driverUsername}");
            }
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails( _context );
            CalcRouteOutput calcOutput = await DeliveryToCalcRouteOutput(driverRoute, dictionaryOrderDetails.OrderDetailsDict);

            //Now need to build the CalcRouteOutput and return that object 

            return calcOutput;
        }
        
        
        /// <summary>
        /// Method <c>ValidateRouteRequest</c>
        /// Confirms is date is future date and all orders exist and are in planned state
        /// </summary>
        /// <param name="routeRequest"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        private async Task<bool> ValidateRouteRequest( RouteRequest routeRequest, StringBuilder sb)
        {
            bool valid = true;
            if( routeRequest.NumVehicle <= 0 )
            {
                valid = false;
                sb.AppendLine($"Number of vehicles too low at {routeRequest.NumVehicle}");
            }
            if (routeRequest.DeliveryDate < DateTime.Today.AddDays(-1) )//Gives a small buffer to add last minute orders
            {
                sb.AppendLine("Date set before current date");
                valid = false;
            }
            var plannedOrders = await _context.Orders
                .Where(order => routeRequest.Orders.Contains(order.Id) && 
                                order.Status == Order.ORDER_STATUSES[0]) //Has "Planned" status, meaning order is not yet assigned a route
                .ToListAsync();

            if ( routeRequest.Orders.Count != plannedOrders.Count)
            {
                sb.AppendLine("One or more orders do not have a 'Planned' status.");
                valid = false;
            }

            return valid;
        }
#endif
//**Generic helper method for either offline or Online Database

        /// <summary>
        /// Method <c>PythonRequest</c> Method to request quantum calculated routes
        /// </summary>
        /// <param name="routesIn"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<RouteRequestListDTO> PythonRequest(CalculatingRoutesDTO routesIn)
        {
            string pythonBackendUrl = "https://quantumdeliverybackend.azurewebsites.net/generate-routes";
            //string pythonBackendUrl = "http://127.0.0.1:8000/generate-routes";
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // Serialize the object to JSON
                    string jsonContent = JsonConvert.SerializeObject(routesIn);

                    // Log the JSON payload
                    Console.WriteLine("JSON Payload Sent to Python:");
                    Console.WriteLine(jsonContent);

                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Post, pythonBackendUrl)
                    {
                        Content = content
                    };

                    // Add a custom header
                    string backend_token = Environment.GetEnvironmentVariable("BACKEND_TOKEN");
                    request.Headers.Add("authorisation", "Bearer " + backend_token);

                    // Send the POST request
                    Console.WriteLine(request.Headers);
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and deserialize the response content
                        string responseContent = await response.Content.ReadAsStringAsync();
                        RouteRequestListDTO routeResponse = JsonConvert.DeserializeObject<RouteRequestListDTO>(responseContent);

                        // Return the deserialized response
                        return routeResponse;
                    }
                    else
                    {
                        // Handle non-successful responses by extracting the error details
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Error from Python backend: {response.ReasonPhrase}. Details: {errorContent}");
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    // Log the exception and throw a custom error
                    Console.WriteLine($"HTTP error: {httpEx.Message}");
                    throw new Exception($"Failed to communicate with the Python backend. {httpEx.Message}.");
                }
                catch (JsonSerializationException jsonEx)
                {
                    // Log JSON serialization/deserialization errors
                    Console.WriteLine($"JSON error: {jsonEx.Message}");
                    throw new Exception("Failed to process the data for the Python backend. Please ensure the data format is correct.");
                }
                catch (Exception ex)
                {
                    // Log any other exceptions
                    Console.WriteLine($"General error: {ex.Message}");
                    throw new Exception($"An unexpected error occurred: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Method <c>PythonOutputToFront</c>
        /// Using Dictionary of OrderDetails and the RouteList object
        /// a List of CalcRouteOutput object are generated. To return detailed
        /// and ordered routes to the front end. 
        /// </summary>
        /// <param name="routeList"></param>
        /// <param name="orderDetailsDict"></param>
        /// <param name="vehicles"></param>
        /// <returns></returns>
        private List<CalcRouteOutput> PythonOutputToFront(RouteRequestListDTO routeList, Dictionary<int, OrderDetailsDTO> orderDetailsDict, List<Vehicle> vehicles)
        {
            //Has a list of list of orderIDs, representing one vehicles routes

            //Full list object to send to frontend, giving all vehicles routes. 
            List<CalcRouteOutput> allRoutesCalced = new List<CalcRouteOutput>();

            for (int i = 0; i < routeList.Count; i++)
            {
                List<int> route = routeList[i];
                CalcRouteOutput routeForFrontend = new CalcRouteOutput();
                List<OrderDetailsDTO> routeDetails = new List<OrderDetailsDTO>();
                routeForFrontend.VehicleId = vehicles[i].LicensePlate;
                //For loops generates an ordered and detailed list of routes for each vehicle
                foreach (int orderID in route)
                {
                    OrderDetailsDTO referenceDetails = orderDetailsDict[orderID];
                    routeDetails.Add(referenceDetails);
                    Console.WriteLine("Added order detail of " + referenceDetails.Address);
                }
                //Vehicle ID assigned by front end after recieving route. 
                routeForFrontend.Orders = routeDetails;
                allRoutesCalced.Add(routeForFrontend);
                Console.WriteLine("Added a route to front end output " + routeForFrontend.ToString());
            }
            return allRoutesCalced;

        }


        /// <summary>
        /// Method <c>PostDeliveryRoute</c>
        /// Uses the DTO or a list of orders and number of vehicles
        /// Collects the location of each order and assigns to send to python
        /// Then convert python output to details needed to front end
        /// Saves the new route in the database then returns
        /// </summary>
        /// <param name="routeRequest"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<List<CalcRouteOutput>>> PostDeliveryRoute(RouteRequest routeRequest)
        {
            await CheckRouteMax(routeRequest);
            StringBuilder sb = new StringBuilder();
            if (!await ValidateRouteRequest(routeRequest, sb))
            {//invalid routeRequest object
                return BadRequest( sb.ToString() );
            }
            //ensures route doesn't out number the available vehicles or drivers
            try
            {
#if OFFLINE_DATA
                Dictionary<int, OrderDetailsDTO> orderDetailsDict = _offlineDatabase.MakeOrdersDictionary();
                // Convert data input to type for Python input
                CalculatingRoutesDTO calcRoute = frontDataToPythonData(routeRequest, orderDetailsDict);
                List<Vehicle> vehicles = _offlineDatabase.Vehicles;

#else
                //Creates a dictionary from the datbase of order details required
                DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
                await dictionaryOrderDetails.GetOrderDetails( _context );

                // Convert data input to type for Python input
                CalculatingRoutesDTO calcRoute = await FrontDataToPythonDataAsync(routeRequest);

                List<Vehicle> vehicles = await _context.Vehicles
                    .Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0])//all active vehicles
                    .ToListAsync();
#endif
                // Make the request to the Python backend
                RouteRequestListDTO routeRequestListDTO = await PythonRequest(calcRoute);

                Console.WriteLine("Returned object from Python is " + routeRequestListDTO.ToString());

                // Convert routeRequestListDTO to CalcRouteOutput
                List<CalcRouteOutput> allRoutesCalced = PythonOutputToFront(routeRequestListDTO, dictionaryOrderDetails.OrderDetailsDict, vehicles);

                Console.WriteLine("All routes calced object is " + allRoutesCalced.ToString());

#if OFFLINE_DATA
                
                AssignPosAndDelivery(allRoutesCalced, routeRequest );
#else
                await AssignPosAndDeliveryAsync(allRoutesCalced);
#endif
                return Ok(allRoutesCalced);
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP-specific exceptions and include detailed error from response
                Console.WriteLine($"HTTP error while processing the route request: {ex.Message}");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
            catch (JsonSerializationException ex)
            {
                // Handle JSON-specific exceptions
                Console.WriteLine($"JSON processing error: {ex.Message}");
                return BadRequest("Invalid data format received. Please check the request.");
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
        }

    }
}
