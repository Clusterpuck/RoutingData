﻿using System;
using System.Collections;
using System.Collections.Generic;
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
    [Authorize]
    public class DeliveryRoutesController : ControllerBase
    {


        //Method to request quantum routes
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



        //converts front end data to the required input for python end point
        private CalculatingRoutesDTO frontDataToPythonData( RouteRequest frontEndData, Dictionary<int, OrderDetail> orderDetailsDict)
        {
            //From routerequest need to make a list of OrderInRoute
            List<OrderInRouteDTO> routesForPython = new List<OrderInRouteDTO>();

            foreach (int orderID in frontEndData.Orders)
            {
                OrderDetail orderDetail = orderDetailsDict[orderID];
                OrderInRouteDTO routeDTO = new OrderInRouteDTO();
                routeDTO.lat = orderDetail.Lat;
                routeDTO.lon = orderDetail.Lon;
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

        private List<CalcRouteOutput> pythonOutputToFront(RouteRequestListDTO routeList, Dictionary<int, OrderDetail> orderDetailsDict)
        {
            //Has a list of list of orderIDs, representing one vehicles routes
            //Full list object to send to frontend, giving all vehicles routes. 
            List<CalcRouteOutput> allRoutesCalced = new List<CalcRouteOutput>();

            for( int i = 0; i < routeList.Count; i++ )
            //foreach (List<int> route in routeList)
            {
                List<int> route = routeList[i];
                CalcRouteOutput routeForFrontend = new CalcRouteOutput();
                List<OrderDetail> routeDetails = new List<OrderDetail>();
                routeForFrontend.VehicleId = i+1;
                //For loops generates an ordered and detailed list of routes for each vehicle
                foreach(int orderID in route )
                {
                    OrderDetail referenceDetails = orderDetailsDict[orderID];
                    routeDetails.Add(referenceDetails);
                    Console.WriteLine("Added order detail of " + referenceDetails.Addr);
                }
                //Vehicle ID assigned by front end after recieving route. 
                routeForFrontend.Orders = routeDetails;
                allRoutesCalced.Add(routeForFrontend);
                Console.WriteLine("Added a route to front end output " + routeForFrontend.ToString());
            }
            return allRoutesCalced;

        }


#if OFFLINE_DATA

        private readonly OfflineDatabase _offlineDatabase;

        public DeliveryRoutesController(OfflineDatabase offlineDatabase)
        {
            _offlineDatabase = offlineDatabase;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<List<CalcRouteOutput>>> PostDeliveryRoute(RouteRequest routeRequest)
        {
            //ensures route doesn't out number the available vehicles or drivers
            checkRouteMax(routeRequest);
            try
            {
                Dictionary<int, OrderDetail> orderDetailsDict = _offlineDatabase.MakeOrdersDictionary();

                // Convert data input to type for Python input
                CalculatingRoutesDTO calcRoute = frontDataToPythonData(routeRequest, orderDetailsDict);

                // Make the request to the Python backend
                RouteRequestListDTO routeRequestListDTO = await PythonRequest(calcRoute);

                Console.WriteLine("Returned object from Python is " + routeRequestListDTO.ToString());

                // Convert routeRequestListDTO to CalcRouteOutput
                List<CalcRouteOutput> allRoutesCalced = pythonOutputToFront(routeRequestListDTO, orderDetailsDict);

                Console.WriteLine("All routes calced object is " + allRoutesCalced.ToString() );

                AssignPosAndDelivery(allRoutesCalced, routeRequest);

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



        // GET: api/DeliveryRoutes
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<DeliveryRoute>>> GetCourses()
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

        private CalcRouteOutput deliveryToCalcRouteOutput( DeliveryRoute deliveryRoute )
        {
            CalcRouteOutput calcRouteOutput = new CalcRouteOutput();
            calcRouteOutput.VehicleId = deliveryRoute.VehicleId;
            //TODO Add conversion

            //dictionary to reference each order to get details
            Dictionary<int, OrderDetail> orderDetailsDict = _offlineDatabase.MakeOrdersDictionary();
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
                    OrderDetail orderDetail = orderDetailsDict[order.Id];
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
            for (int i = 0; i < routeRequest.NumVehicle; i++)
            {
                DeliveryRoute newRoute = new DeliveryRoute();
                newRoute.Id = _offlineDatabase.deliveryRoutes.Any() ?
                                _offlineDatabase.deliveryRoutes.Last().Id + 1 : 1;

                newRoute.DeliveryDate = DateTime.Today;

                newRoute.VehicleId = _offlineDatabase.Vehicles[i].Id;
                newRoute.DriverUsername = _offlineDatabase.Drivers[i].Username;

                //also need to add position number for each order. 
                //for each orderID in the List of Order Details in the corresponding CalcRouteOutput object in allRoutesCalced
                //need to find the matching order object in offlinedatabase and assign the routeID
                _offlineDatabase.deliveryRoutes.Add(newRoute);
                int pos = 1;
                foreach (OrderDetail order in allRoutesCalced[i].Orders)
                {
                    int orderID = order.OrderId;
                    Order dbOrder = ordersDict[orderID];
                    dbOrder.DeliveryRouteId = newRoute.Id;
                    dbOrder.PositionNumber = pos;
                    pos++;
                }

            }

        }

        
        private void checkRouteMax(RouteRequest routeRequest)
        {
            // Find the minimum of the driver count and vehicle count
            int maxVehicles = Math.Min(_offlineDatabase.Drivers.Count, _offlineDatabase.Vehicles.Count);

            // Only assign the minimum value if the current NumVehicle exceeds it
            if (routeRequest.NumVehicle > maxVehicles)
            {
                routeRequest.NumVehicle = maxVehicles;
            }
        }


        



#else
        private readonly ApplicationDbContext _context;

        public DeliveryRoutesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DeliveryRoutes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DeliveryRoute>>> GetCourses()
        {
          if (_context.Courses == null)
          {
              return NotFound();
          }
            return await _context.Courses.ToListAsync();
        }

        // GET: api/DeliveryRoutes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DeliveryRoute>> GetDeliveryRoute(int id)
        {
          if (_context.Courses == null)
          {
              return NotFound();
          }
            var deliveryRoute = await _context.Courses.FindAsync(id);

            if (deliveryRoute == null)
            {
                return NotFound();
            }

            return deliveryRoute;
        }

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

            return NoContent();
        }

        // POST: api/DeliveryRoutes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<DeliveryRoute>> PostDeliveryRoute(RouteRequest routeRequest )
        {
          if (_context.Courses == null)
          {
              return Problem("Entity set 'ApplicationDbContext.Courses'  is null.");
          }


            // Create the object list for Python. Need to assign lat and lon from
            // list of orderIDs
            // Fetch all orders asynchronously
            List<Order> allOrders = await _context.Orders.ToListAsync();
            List<RoutingData.Models.Location> locations = await _context.Locations.ToListAsync();
            List<OrderProduct> orderProducts = await _context.OrderProducts.ToListAsync();
            List<Product> products = await _context.Products.ToListAsync();

            List<OrderInRouteDTO> ordersInRoute = new List<OrderInRouteDTO>();

            //This is very cringe and would ideally be handled by database calls
            //However need to try to limit usage
            foreach ( int orderID in routeRequest.Orders)
            {//create an from each order id for each order id sent
                OrderInRouteDTO order = new OrderInRouteDTO();
                order.order_id = orderID;
                Order matchOrder = allOrders.FirstOrDefault(o => o.Id == orderID);
                int locationID = matchOrder.LocationId;
                RoutingData.Models.Location orderLocation = locations.FirstOrDefault(o => o.Id == locationID);
                order.lat = orderLocation.Latitude;
                order.lon = orderLocation.Longitude;

                ordersInRoute.Add(order);
            }

            //Now built a list of OrderInROute, can make object to send to python

            CalculatingRoutesDTO routeToCalc = new CalculatingRoutesDTO();
            routeToCalc.orders = ordersInRoute;
            routeToCalc.num_vehicle = routeToCalc.num_vehicle;
            //Send to python, await response which should be a RouteRequestListDTO object
            //using endpoint https://quantumdeliverybackend.azurewebsites.net/generate-routes

            // Send to Python, await response which should be a RouteRequestListDTO object

            try
            {
                RouteRequestListDTO routeResponse = await PythonRequest(routeToCalc);
                //need to now convert routeResponse to the FrontEndInput function
                //So needs orderdetail objects
                List<CalcRouteOutput> calcRouteOutputs = convertToFrontEndOutput(locations, allOrders, orderProducts, products, routeResponse);
                return CreatedAtAction("GetDeliveryRoute", calcRouteOutputs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        /*Add later to also save to database
         * _context.Courses.Add(deliveryRoute);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDeliveryRoute", new { id = deliveryRoute.Id }, deliveryRoute);*/

           
        }

        private List<CalcRouteOutput> convertToFrontEndOutput( List<Location> locations, List<Order> orders, 
                                                               List<OrderProduct> orderProducts, List<Product> products,
                                                               RouteRequestListDTO routes )
        {
            List<CalcRouteOutput> output = new List<CalcRouteOutput>();
            //Iterate through the list of lists. For each list create a new CalcRouteOutput object, with a list of order details
            foreach (List<int> orderList in routes.orders)
            {
                List<OrderDetail> orderDetailList = new List<OrderDetail>();
                foreach (int orderID in orderList)
                {
                    OrderDetail orderDetail = new OrderDetail();
                    Order matchOrder = orders.FirstOrDefault(o => o.Id == orderID);
                    int locationID = matchOrder.LocationId;
                    Location orderLocation = locations.FirstOrDefault(o => o.Id == locationID);

                    orderDetail.OrderId = orderID;

                    //Details from location list
                    orderDetail.Addr = orderLocation.Address;
                    orderDetail.Lat = orderLocation.Latitude;
                    orderDetail.Long = orderLocation.Longitude;
                    orderDetail.Status = "Planned";

                    //Details from OrderProductList
                    //Need to first get the order product list

                    //For each orderproduct that matches the orderID
                    //Find the product name from the products list
                    //Add to a List<String> to then store as ProdNames

                    List<string> prodNames = new List<string>();

                    foreach (OrderProduct orderProduct in orderProducts) {
                        if (orderProduct.OrderId == orderID)
                        {
                            int productID = orderProduct.ProductId;
                            Product product = products.FirstOrDefault(o => o.Id == productID);

                            prodNames.Add(product.Name);
                        }
                    }

                    orderDetail.ProdNames = prodNames;

                    orderDetailList.Add(orderDetail);

                }
                CalcRouteOutput newRoute = new CalcRouteOutput();
                newRoute.Orders = orderDetailList;
                newRoute.VehicleId = 1;
                output.Add(newRoute);
            }

            return output;
        }




        //Method to request quantum routes
        private async Task<RouteRequestListDTO> PythonRequest(CalculatingRoutesDTO routesIn)
        {
            string pythonBackendUrl = "https://quantumdeliverybackend.azurewebsites.net/generate-routes";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    // Serialize the object to JSON
                    string jsonContent = JsonConvert.SerializeObject(routesIn);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Send the POST request
                    HttpResponseMessage response = await httpClient.PostAsync(pythonBackendUrl, content);

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
                        // Handle non-successful responses
                        throw new Exception($"Error from Python backend: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions that occur during the HTTP call
                    // Log the exception if needed
                    throw new Exception($"Internal server error: {ex.Message}");
                }
            }
        }


        // DELETE: api/DeliveryRoutes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDeliveryRoute(int id)
        {
            if (_context.Courses == null)
            {
                return NotFound();
            }
            var deliveryRoute = await _context.Courses.FindAsync(id);
            if (deliveryRoute == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(deliveryRoute);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DeliveryRouteExists(int id)
        {
            return (_context.Courses?.Any(e => e.Id == id)).GetValueOrDefault();
        }
#endif
    }
}
