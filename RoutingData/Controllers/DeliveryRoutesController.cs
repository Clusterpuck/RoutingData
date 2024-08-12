using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RoutingData.DTO;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryRoutesController : ControllerBase
    {
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
            List<Location> locations = await _context.Locations.ToListAsync();
            List<OrderProduct> orderProducts = await _context.OrderProducts.ToListAsync();
            List<Product> products = await _context.Products.ToListAsync();

            List<OrderInRouteDTO> ordersInRoute = new List<OrderInRouteDTO>();

            //This is very cringe and would ideally be handled by database calls
            //However need to try to limit usage
            foreach ( int orderID in routeRequest.Orders)
            {//create an from each order id for each order id sent
                OrderInRouteDTO order = new OrderInRouteDTO();
                order.order_ID = orderID;
                Order matchOrder = allOrders.FirstOrDefault(o => o.Id == orderID);
                int locationID = matchOrder.LocationId;
                Location orderLocation = locations.FirstOrDefault(o => o.Id == locationID);
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
    }
}
