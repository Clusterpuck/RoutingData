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
        [Authorize]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderStatus(OrderStatusDTO orderStatusDTO)
        {
            //Change to ignore case
            orderStatusDTO.Status = orderStatusDTO.Status.ToUpper();
            //Check if a valid status provided before using ay database requests
            if( !Order.ORDER_STATUSES.Contains(orderStatusDTO.Status) )
            {
                string availableStatuses = string.Join(", ", Order.ORDER_STATUSES);
                return BadRequest($"Invalid status sent of {orderStatusDTO.Status} +" +
                    $" Must be either one of: {availableStatuses}");
            }

            // check if the order exists and is part of the driver's delivery route
            var order = await _context.Orders
                              .FirstOrDefaultAsync(o => o.Id == orderStatusDTO.OrderId );

            if (order == null)
            {
                return NotFound("No matching order in that driver's route");
            }

            // find the route mathing that order. 
            var driverRoute = await _context.DeliveryRoutes
                                    .FirstOrDefaultAsync(route => 
                                    (route.Id == order.DeliveryRouteId ) && (route.DriverUsername.Equals(orderStatusDTO.Username)));

            if (driverRoute == null)
            {
                return NotFound("No route assigned to provided driver");
            }

            // update the order status
            try
            {
                order.ChangeStatus(orderStatusDTO.Status);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Error in changing order's state: {ex.Message}");
            }

            // check if any orders in the route must be set to delayed

            //get orders in the route
            var routeOrders = await _context.Orders
                                    .Where(o => o.DeliveryRouteId == driverRoute.Id)
                                    .ToListAsync();

            foreach (var routeOrder in routeOrders)
            {
                // check if the delivery time is more than 1 hour past the current time
                if (routeOrder.DeliveryDate.AddHours(1) < DateTime.Now)
                {
                    routeOrder.Delayed = true;
                    _context.Orders.Update(routeOrder);
                }
            }

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            //each status update request should return the relevent route it was updated for
            // retrieve all orders for the delivery route
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails(_context);
            CalcRouteOutput routeForFrontend = await DeliveryToCalcRouteOutput(driverRoute, dictionaryOrderDetails.OrderDetailsDict);


            return Ok(routeForFrontend);
        }

        // amira added
        /// <summary>
        /// Method <c>UpdateOrderDelayed</c> 
        /// </summary>
        /// <param name="orderDelayedDTO"></param>
        /// <returns></returns>
        [HttpPost("update-delayed")]
        [Authorize]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderDelayed(OrderDelayedDTO orderDelayedDTO)
        {
            //updated to be non-case sensitive and only accept true. Can't go from true to false for delayed
            string boolString = orderDelayedDTO.Delayed.ToLowerInvariant();
            //Check if a valid boolean provided before using any database requests
            if ( !( boolString.Equals("true") ) )
            {
                return BadRequest($"Invalid Delayed value sent of {orderDelayedDTO.Delayed} +" +
                    " Can only set to true.");
            }

            // check if the order exists
            var order = await _context.Orders
                              .FirstOrDefaultAsync(o => o.Id == orderDelayedDTO.OrderId);

            if (order == null)
            {
                return NotFound($"No matching order for the given ID: {orderDelayedDTO.OrderId}");
            }

            // check if the delivery route matches the provided driver
            var driverRoute = await _context.DeliveryRoutes
                                    .FirstOrDefaultAsync(route => 
                                        ( 
                                            (route.Id == order.DeliveryRouteId ) && 
                                            ( route.DriverUsername.Equals( orderDelayedDTO.Username ) ) ) 
                                        );

            if (driverRoute == null )
            {
                return NotFound("No matching route assigned to provided driver");
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

        // amira added
        /// <summary>
        /// Method <c>UpdateOrderIssue</c> 
        /// </summary>
        /// <param name="orderIssueDTO"></param>
        /// <returns></returns>
        [HttpPost("update-issue")]
        [Authorize]
        public async Task<ActionResult<CalcRouteOutput>> UpdateOrderIssue(OrderIssueDTO orderIssueDTO)
        {
            //Check if a valid message is provided before using any database requests
            if (string.IsNullOrEmpty(orderIssueDTO.DriverNote))
            {
                return BadRequest($"DriverNote field is empty. Must provide a note on the issue.");
            }

            // check if the order exists 
            var order = await _context.Orders
                              .FirstOrDefaultAsync(o => o.Id == orderIssueDTO.OrderId );

            if (order == null)
            {
                return NotFound("No matching order in that driver's route");
            }

            // check if the delivery route exists for the driver
            var driverRoute = await _context.DeliveryRoutes
                                    .FirstOrDefaultAsync(route => ( route.Id == order.DeliveryRouteId ) && 
                                    ( route.DriverUsername == orderIssueDTO.Username ) );

            if (driverRoute == null)
            {
                return NotFound("No route assigned to provided driver");
            }

            // order must be on route to set the status to 'issue'
            if (!(order.Status.Equals(Order.ORDER_STATUSES[1])))
            {
                return BadRequest("Order must be on-route to change the status to issue.");
            }

            // update the orders status attribute
            try
            {
                order.ChangeStatus(Order.ORDER_STATUSES[5]);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Error in changing order's state: {ex.Message}");
            }
            // add the message to the order notes
            string currentDateTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
            order.OrderNotes += $" | {currentDateTime} Driver Note: {orderIssueDTO.DriverNote}";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            // returns the relevent route that was reported
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
        private async Task<CalculatingRoutesDTO> FrontDataToPythonDataAsync(RouteRequest frontEndData, RoutingData.Models.Location depot)
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

            CalculatingRoutesDTO calcRoute = BuildCalcRoute(frontEndData, routesForPython, depot);

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
        private CalculatingRoutesDTO BuildCalcRoute(RouteRequest frontEndData, List<OrderInRouteDTO> routesForPython, RoutingData.Models.Location depot)
        {
            CalculatingRoutesDTO calcRoute = new CalculatingRoutesDTO
            {
                orders = routesForPython,
                vehicle_cluster_config = new SubCalcSetting
                {
                    type = frontEndData.Type,
                    k = frontEndData.NumVehicle,
                    k_max = frontEndData.NumVehicle,
                    k_init = 1
                },
                solver_config = new SolverCalcSetting
                {
                    type = "brute",//frontEndData.CalcType, // 
                    distance = frontEndData.Distance,
                    max_solve_size = 5
                },
                // Ideally, search Db for some depot location. For now, hardcoded depot near Uni
                depot = new Depot
                {
                    lat = depot.Latitude,
                    lon = depot.Longitude,
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
        [Authorize]
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
                try
                {
                    order.ChangeStatus(Order.ORDER_STATUSES[1]); //assigned On-Route
                }
                catch (ArgumentException ex)
                {
                    return BadRequest($"Error in changing order's state: {ex.Message}");
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Created("", startRoute);
        }



        /// <summary>
        /// Assigns a driver to the given routeID
        /// If driver is assigned to a different route on the same day, they are swapped
        /// </summary>
        /// <param name="routeID"></param>
        /// <param name="driver"></param>
        /// <returns></returns>
        [HttpPut("assign-driver/{routeID}/{driver}")]
        [Authorize]
        public async Task<IActionResult> PutDeliveryDriver(int routeID, string driver)
        {
            //Check driver exists
            Account inDriver = await _context.Accounts.
                Where(account => (account.Role == Account.ACCOUNT_ROLES[0])).
                FirstOrDefaultAsync( account => (account.Username.Equals(driver)));
            if (inDriver == null)
            {
                return BadRequest("Driver doesn't exist");
            }
            DeliveryRoute routeAssigning = await _context.DeliveryRoutes.
                FirstOrDefaultAsync(route => route.Id == routeID);
            if (routeAssigning == null)
            {
                return NotFound("Delivery route id not in database");
            }
            //Get the date of the route
            DateTime routeDate = routeAssigning.DeliveryDate;
            
            //Get all other routes on this date
            List<DeliveryRoute> routesOnDay = await _context.DeliveryRoutes.
                    Where(route => (route.DeliveryDate.Date == routeDate.Date)).
                    ToListAsync();

            if (routesOnDay.Count == 1)
            {//only one route, not need to swap any other
                routeAssigning.DriverUsername = driver;
                await _context.SaveChangesAsync();
                return Ok(routeAssigning);
            }
            else
            {
            // Check if driver is busy (assigned to another route on the same day)
                bool driverBusy = routesOnDay
                    .FirstOrDefault(route => (route.DriverUsername.Equals(driver)) && (route.Id != routeID)) != null;


                if (!driverBusy)
                {//driver not assigned to any other routes
                    routeAssigning.DriverUsername = driver;
                    await _context.SaveChangesAsync();
                    return Ok(routeAssigning);
                }
                else
                {
                    // Driver is already assigned to another route, attempt to find another route for swapping
                    DeliveryRoute nextRoute = routesOnDay
                        .FirstOrDefault(route => !route.DriverUsername.Equals(driver) && route.Id != routeID);

                    if (nextRoute != null)
                    {//found another route to give other driver
                        nextRoute.DriverUsername = routeAssigning.DriverUsername;
                        routeAssigning.DriverUsername = driver;
                    }
                    else
                    {//no other routes to swap driver to, jut replace
                        routeAssigning.DriverUsername = driver;
                    }

                    await _context.SaveChangesAsync();
                    return Ok(routeAssigning);


                }
            }

        }

        /// <summary>
        /// Method <c>GetdeliveryRoutes</c>
        /// returns a list of full detail routes of all routes in the system. 
        /// </summary>
        /// <returns></returns>
        // GET: api/DeliveryRoutes
        [HttpGet]
        [Authorize]
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
        /// Method <c>GetdeliveryRoutes</c>
        /// returns a list of full detail routes of all routes in the system. 
        /// </summary>
        /// <returns></returns>
        // GET: api/DeliveryRoutes
        [HttpGet("active")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<CalcRouteOutput>>> GetActiveDeliveryRoutes()
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
                CalcRouteOutput routeOutput = await DeliveryToCalcRouteOutputActive(route, dictionaryOrderDetails.OrderDetailsDict);
                if (routeOutput != null)
                {//previous returns null it is not an active route
                    routesDetailed.Add(routeOutput);
                }

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
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> PutDeliveryRoute(int id, UpdateRouteDTO routeDTO)
        {
            if (id != routeDTO.routeID) // checking IDs match
            {
                return BadRequest(" Route ID mismatch");
            }

            if (_context.DeliveryRoutes == null) // checking routes exist
            {
                return NotFound("Delivery routes not found.");
            }

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Username == routeDTO.driverUsername);
            if (account == null || account.Status == Account.ACCOUNT_STATUSES[1])
            {
                return BadRequest("The account is inactive and cannot be assigned a route.");
            }

            if (account.Role != Account.ACCOUNT_ROLES[0])
            {
                return BadRequest("The account does not have the 'driver' role and cannot be assigned a route.");
            }

            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(a => a.LicensePlate == routeDTO.vehicleID);
            if (vehicle == null || vehicle.Status == Vehicle.VEHICLE_STATUSES[1])
            {
                return BadRequest("The vehicle is inactive and cannot be assigned a route.");
            }



            var route = await _context.DeliveryRoutes.FirstOrDefaultAsync(a => a.Id == id);

            if (route == null)
            {
                return NotFound("The specified route was not found.");
            }

            // Check if the driver or vehicle is already assigned a route on the same date
            var existingRouteForDriver = await _context.DeliveryRoutes
                .Where(r => r.DriverUsername == routeDTO.driverUsername && r.DeliveryDate == route.DeliveryDate && r.Id != id)
                .FirstOrDefaultAsync();

            if (existingRouteForDriver != null)
            {
                return BadRequest("The driver is already assigned to a route on this date.");
            }

            var existingRouteForVehicle = await _context.DeliveryRoutes
                .Where(r => r.VehicleLicense == routeDTO.vehicleID && r.DeliveryDate == route.DeliveryDate && r.Id != id)
                .FirstOrDefaultAsync();

            if (existingRouteForVehicle != null)
            {
                return BadRequest("The vehicle is already assigned to a route on this date.");
            }

            var routeOrders = await _context.Orders
                                    .Where(o => o.DeliveryRouteId == route.Id)
                                    .ToListAsync();

            foreach (var routeOrder in routeOrders)
            {
                // check that each order has status ASSIGNED
                if (routeOrder.Status != Order.ORDER_STATUSES[4])
                {
                    return BadRequest("All orders in the delivery route must have status ASSIGNED to update driver / vehicle details.");
                }
            }

            // Check if the driver or vehicle details have changed
            if (route.DriverUsername == routeDTO.driverUsername && route.VehicleLicense == routeDTO.vehicleID)
            {
                return BadRequest("No changes were made because the driver and vehicle details entered are the same as the current assigned details.");
            }

            route.DriverUsername = routeDTO.driverUsername;
            route.VehicleLicense = routeDTO.vehicleID;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                return Problem($"An error occurred while updating the route: {ex.Message}");
            }

            return NoContent();
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
                Console.WriteLine("Assigning vehicle " + vehicles[i].LicensePlate);
                // Create and assing values to a new DeliveryRoute object
                var newRoute = new DeliveryRoute
                {
                    DeliveryDate = allRoutesCalced[i].DeliveryDate,
                    VehicleLicense = vehicles[i].LicensePlate,
                    DriverUsername = drivers[i].Username,
                    TimeCreated = DateTime.Now,
                    //defaults to -1 if Depot was not assigned correctly
                    DepotID =  allRoutesCalced[i].Depot != null ? allRoutesCalced[i].Depot.Id : -1,
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
                        try
                        {
                            dbOrder.ChangeStatus(Order.ORDER_STATUSES[4]); //Status of order set to assigned
                        }
                        catch (ArgumentException)
                        {
                            throw; // Invalid state transition
                        }
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
        /// And sets their position and routeIds to -1 before removing the entity from database.
        /// It prevents deletion if any orders are marked as delivered.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE: api/DeliveryRoutes/5
        [HttpDelete("{id}")]
        [Authorize]
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

            // First, get the list of orders assigned to that route
            List<Order> ordersInRoute = await _context.Orders
                .Where(order => order.DeliveryRouteId == deliveryRoute.Id)
                .ToListAsync();

            // Check if any of the orders have the status "delivered"
            if (ordersInRoute.Any(order => order.Status == Order.ORDER_STATUSES[2]))
            {
                return BadRequest("Cannot delete route because some orders are marked as delivered.");
            }

            // If no orders are delivered, proceed to update and delete the route
            foreach (var order in ordersInRoute)
            {
                try
                {
                    order.ChangeStatus(Order.ORDER_STATUSES[0]); // Changes back to "planned"
                }
                catch (ArgumentException ex)
                {
                    return BadRequest($"Error in changing order's state: {ex.Message}");
                }

                // Reset the order's delivery route and position
                order.DeliveryRouteId = -1;
                order.PositionNumber = -1;
                order.Delayed = false; // Reset delayed status to false

                // Mark the order as modified
                _context.Orders.Update(order);
            }

            // Remove the delivery route
            _context.DeliveryRoutes.Remove(deliveryRoute);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Route deleted successfully" });
        }



        [HttpDelete("date/{date}")]
        [Authorize]
        public async Task<IActionResult> DeleteRouteByDate(DateTime date)
        {
            if (_context.DeliveryRoutes == null)
            {
                return NotFound();
            }

            //Need to also select only routes that have all orders in ASSIGNED or CANCELLED status
            List<DeliveryRoute> deliveryRoutes = await _context.DeliveryRoutes
                .Where(route => route.DeliveryDate.Date == date.Date)
                .Where(route => _context.Orders
                    .Where(order => order.DeliveryRouteId == route.Id)
                    .All(order => order.Status == "ASSIGNED" || order.Status == "CANCELLED"))
                .ToListAsync();

            if ( deliveryRoutes.IsNullOrEmpty() )
            {//no routes that aren't started
                return NotFound();
            }

            //to -1 to effectively delete
            //First need the list of orders assigned to the routes

            List<int> routeIds = deliveryRoutes.Select( route => route.Id).ToList();

            List<Order> ordersInRoute = await _context.Orders.
                 Where(order => routeIds.Contains(order.DeliveryRouteId)). //all orders in all routes
                 ToListAsync();

            foreach (var order in ordersInRoute)
            {
                try
                {
                    order.ChangeStatus(Order.ORDER_STATUSES[0]); //changes back to planned
                }
                catch (ArgumentException ex)
                {//this should be absolute last resort check.
                    //As at this point some orders in the route will already be changed
                    return BadRequest($"Error in changing order's state: {ex.Message}");
                }
                order.DeliveryRouteId = -1;
                order.PositionNumber = -1;
                order.Delayed = bool.Parse("false"); // change delayed status back to false

                //Marking order as modified
                _context.Orders.Update(order);
            }
            int count = 0;
            foreach( DeliveryRoute route in deliveryRoutes)
            {
                _context.DeliveryRoutes.Remove(route);
                ++count;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{count} routes deleted successfully" }); // Return a success message
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
            calcRouteOutput.Depot = await _context.Locations.
                FirstOrDefaultAsync(location => location.Id == deliveryRoute.DepotID);
            int routeID = deliveryRoute.Id;
            calcRouteOutput.DeliveryRouteID = routeID;
            calcRouteOutput.DeliveryDate = deliveryRoute.DeliveryDate;

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

        /// <summary>
        /// Method <c>DeliveryToCalcRouteOutput</c>
        /// Using the OrderDetails dictionary, populates a CalcRouteOutput object
        /// For the front end to have the required details
        /// </summary>
        /// <param name="deliveryRoute"></param>
        /// <returns></returns>
        private async Task<CalcRouteOutput> DeliveryToCalcRouteOutputActive(DeliveryRoute deliveryRoute, Dictionary<int, OrderDetailsDTO> orderDetailsDict)
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
            calcRouteOutput.DeliveryDate = deliveryRoute.DeliveryDate;
            calcRouteOutput.Depot = await _context.Locations.FirstOrDefaultAsync(location => location.Id == deliveryRoute.DepotID );

            foreach (Order order in orders)
            {
                if (order.DeliveryRouteId == routeID)
                {//Finding the matching orderDetail that belongs to the route
                    //Adding to the calcRouteOutput
                    OrderDetailsDTO orderDetail = orderDetailsDict[order.Id];
                    calcRouteOutput.Orders.Add(orderDetail);

                }

            }
            foreach (OrderDetailsDTO orderDet in calcRouteOutput.Orders)
            {//If any order in the route is Assigned or On-route then route is active
                if( orderDet.Status == Order.ORDER_STATUSES[1] || orderDet.Status == Order.ORDER_STATUSES[4])
                {
                    return calcRouteOutput;

                }

            }

            //returns null if route isn't active
            return null;

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
        [Authorize]
        public async Task<ActionResult<CalcRouteOutput>> GetDeliveryRoutesByDriver(string driverUsername)
        {
            if (!IsValidEmail(driverUsername) )
            {
                return BadRequest("Email is not a valid format");
            }

            var driverRoutes = await _context.DeliveryRoutes
                                    .Where(r => r.DriverUsername == driverUsername)
                                    .ToListAsync();

            if (!driverRoutes.Any())
            {
                return NotFound($"No delivery routes found for driver with username {driverUsername}");
            }
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails( _context );

            var calcRouteOutputs = new List<CalcRouteOutput>();

            foreach (var route in driverRoutes)
            {
                var calcOutput = await DeliveryToCalcRouteOutput(route, dictionaryOrderDetails.OrderDetailsDict);
                calcRouteOutputs.Add(calcOutput);
            }

            //Now need to build the CalcRouteOutput and return that object 

            return Ok(calcRouteOutputs);
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
            var startOfDay = routeRequest.DeliveryDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1); // End of the day

            var plannedOrders = await _context.Orders
                .Where(order => routeRequest.Orders.Contains(order.Id) &&
                    order.Status == Order.ORDER_STATUSES[0] && // Has "Planned" status
                    order.DeliveryDate >= startOfDay && order.DeliveryDate <= endOfDay) // Matching DeliveryDate range
                .ToListAsync();

            if (routeRequest.Orders.Count != plannedOrders.Count)
            {
                sb.AppendLine($"There are {plannedOrders.Count} planned orders but {routeRequest.Orders.Count} orders requested. One or more orders do not have a 'Planned' status or are not the same date as orders.");
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
        private List<CalcRouteOutput> PythonOutputToFront(RouteRequestListDTO routeList, Dictionary<int, OrderDetailsDTO> orderDetailsDict, 
                                                            List<Vehicle> vehicles)
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


        //This is essentailly a copy of delete by date, but needs to return the deleted order ID
        //And can't return a BadRequest as not intended as a endpoint
        private async Task<int> RemoveExistingRoutes( DateTime date, List<int> ordersRemoved )
        {//get all routes on the same date as the routerequest
         //Need to also select only routes that have all orders in ASSIGNED or CANCELLED status

            List<DeliveryRoute> deliveryRoutes = await _context.DeliveryRoutes
                .Where(route => route.DeliveryDate.Date == date.Date)
                .Where(route => _context.Orders
                    .Where(order => order.DeliveryRouteId == route.Id)
                    .All(order => order.Status == "ASSIGNED" || order.Status == "CANCELLED"))
                .ToListAsync();
            //If no routes found, that is fine, just continue by doing nothing

            List<int> routeIds = deliveryRoutes.Select(route => route.Id).ToList();

            List<Order> ordersInRoute = await _context.Orders.
                 Where(order => routeIds.Contains(order.DeliveryRouteId)). //all orders in all routes
                 ToListAsync();

            foreach (var order in ordersInRoute)
            {
                try
                {
                    order.ChangeStatus(Order.ORDER_STATUSES[0]); //changes back to planned
                    ordersRemoved.Add(order.Id);
                }
                catch (ArgumentException ex)
                {//this should be absolute last resort check.
                    //As at this point some orders in the route will already be changed
                    return 0;
                }
                order.DeliveryRouteId = -1;
                order.PositionNumber = -1;
                order.Delayed = false; // change delayed status back to false

                //Marking order as modified
                _context.Orders.Update(order);
            }
            int count = 0;
            foreach (DeliveryRoute route in deliveryRoutes)
            {
                _context.DeliveryRoutes.Remove(route);
                ++count;
            }

            await _context.SaveChangesAsync();
            return count;

            //delete those routes and add the orders to the routeRequest. 


        }

        private async Task<RoutingData.Models.Location> DepotExists( int DepotID )
        {
            RoutingData.Models.Location foundDepot = await _context.Locations.
                FirstOrDefaultAsync(location => (location.Id == DepotID && location.IsDepot));
            return foundDepot;

        }

        //Test with
        /*     {
       "numVehicle": 2,
       "calcType": "k",
       "distance": "cartesian",
       "type": "kmeans",
       "depot": 81,
       "deliveryDate": "2024-11-15T15:20:00",
       "orders": [
         218
       ]
         }*/

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
        [Authorize]
        public async Task<ActionResult<List<CalcRouteOutput>>> PostDeliveryRoute(RouteRequest routeRequest)
        {
            await CheckRouteMax(routeRequest);
            StringBuilder sb = new StringBuilder();

            if (!await ValidateRouteRequest(routeRequest, sb))
            {//invalid routeRequest object
                return BadRequest( sb.ToString() );
            }

            RoutingData.Models.Location routeDepot = await DepotExists(routeRequest.Depot);
            if (routeDepot == null)
            {
                return BadRequest("Depot was not valid");
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
                List<int> olderOrders = new();
                //gets all the routes on the date and deletes. return the orders now freed up. 
                int newVehicles = await RemoveExistingRoutes(routeRequest.DeliveryDate, olderOrders);

                //vehicles freed up from route now included in the new request
                routeRequest.NumVehicle += newVehicles;

                //Adds the older oders to the request, avoiding duplicates
                routeRequest.Orders = routeRequest.Orders.Union(olderOrders).ToList();

                //Creates a dictionary from the datbase of order details required
                DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
                await dictionaryOrderDetails.GetOrderDetails( _context );

                // Convert data input to type for Python input
                CalculatingRoutesDTO calcRoute = await FrontDataToPythonDataAsync(routeRequest, routeDepot);

                List<Vehicle> vehicles = await _context.Vehicles
                    .Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0])//all active vehicles
                    .ToListAsync();
#endif
                // Make the request to the Python backend
                RouteRequestListDTO routeRequestListDTO = await PythonRequest(calcRoute);

                Console.WriteLine("Returned object from Python is " + routeRequestListDTO.ToString());

                // Convert routeRequestListDTO to CalcRouteOutput
                List<CalcRouteOutput> allRoutesCalced = PythonOutputToFront(routeRequestListDTO, dictionaryOrderDetails.OrderDetailsDict, vehicles);
                //assign the delivery date to all the routes and the depot
                foreach( var route in allRoutesCalced )
                {
                    route.DeliveryDate = routeRequest.DeliveryDate;
                    route.Depot = routeDepot;
                }

                Console.WriteLine("All routes calced object is " + allRoutesCalced[0].ToString());

#if OFFLINE_DATA
                
                AssignPosAndDelivery(allRoutesCalced, routeRequest );
#else
                try
                {
                    await AssignPosAndDeliveryAsync(allRoutesCalced);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest($"Error in changing order's state: {ex.Message}");
                }
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
