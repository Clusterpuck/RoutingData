using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoutingData.DTO;
using RoutingData.Models;

namespace RoutingData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalculationController : Controller
    {
        public readonly ApplicationDbContext _context;

        public CalculationController( ApplicationDbContext context)
        {

            _context = context; 
        }

        // GET: api/CalculationStatus/{id}
        //To be used to determine if any calculations at all are running
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<int>> GetCalculationsCount()
        {
            if (_context.Calculations == null)
            {
                return NotFound();
            }
            //return if any calculation is running the number of calculations
            int count =  await _context.Calculations.
                Where( calculation => calculation.Status == Calculation.CALCULATION_STATUS[1]).
                CountAsync();

            return count;
        }

        // POST: api/Calculation
        // Initiates a new calculation and returns the unique request ID
        [HttpPost]
        public async Task<ActionResult<Calculation>> PostCalculation(RouteRequestListDTO calcResult)
        {
            //Grab the calculation component that is set to CALCULATING
            Calculation calculation = await _context.Calculations.
                Where(calculation => calculation.Status == Calculation.CALCULATION_STATUS[1]).
                FirstOrDefaultAsync();

            if( calculation == null)
            {
                return BadRequest("No calculations were running");
            }

            //Creates a dictionary from the datbase of order details required
            DictionaryOrderDetails dictionaryOrderDetails = new DictionaryOrderDetails();
            await dictionaryOrderDetails.GetOrderDetails(_context);

            List<Vehicle> vehicles = await _context.Vehicles
                   .Where(vehicle => vehicle.Status == Vehicle.VEHICLE_STATUSES[0])//all active vehicles
                   .ToListAsync();

            calculation.Status = Calculation.CALCULATION_STATUS[0];
            calculation.EndTime = DateTime.Now;

            Console.WriteLine("Returned object from Python is " + calcResult.ToString());

            // Convert routeRequestListDTO to CalcRouteOutput
            List<CalcRouteOutput> allRoutesCalced = PythonOutputToFront(calcResult, dictionaryOrderDetails.OrderDetailsDict, vehicles);
            //assign the delivery date to all the routes and the depot
            foreach (var route in allRoutesCalced)
            {
                route.DeliveryDate = calculation.DeliveryDate;
               // route.Depot = routeDepot;
            }


            try
            {
                await AssignPosAndDeliveryAsync(allRoutesCalced, _context);
                // Mark the calculation as completed and update the end time
                calculation.Status = Calculation.CALCULATION_STATUS[0]; // "COMPLETED"
                calculation.EndTime = DateTime.Now;

            }
            catch (ArgumentException ex)
            {
                calculation.Status = Calculation.CALCULATION_STATUS[2]; // calculation failed
                calculation.ErrorMessage += $"Argument Error: {ex.Message}\n"; // Append error message
            }

            _context.Entry(calculation).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Return the created calculation object, including its unique ID
            return CreatedAtAction(nameof(GetCalculationById), new { id = calculation.ID }, calculation);
        }



        /// <summary>
        /// Method <c>AssignPosAndDeliveryAsync</c> Assigns vehicle in order available
        /// Need to consider vehicle availability in the future
        /// Date as today and driver in order active
        /// Then position and route id assigned to each order, in order in each list
        /// </summary>
        /// <param name="allRoutesCalced"></param>
        /// <returns></returns>
        private async Task AssignPosAndDeliveryAsync(List<CalcRouteOutput> allRoutesCalced, ApplicationDbContext scopedContext)
        {
            Console.WriteLine("Entering Assign Position and Delivery");
            // Fetch all necessary data from the database

            var ordersDict = await scopedContext.Orders.
                Where(order => order.Status == Order.ORDER_STATUSES[0]).
                ToDictionaryAsync(o => o.Id);
            var drivers = await scopedContext.Accounts.
                Where(account => (account.Role == Account.ACCOUNT_ROLES[0] && //Driver roles only 
                    account.Status == Account.ACCOUNT_STATUSES[0])). //Active Drivers only
                ToListAsync();
            var vehicles = await scopedContext.Vehicles.
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
                    DepotID = allRoutesCalced[i].Depot != null ? allRoutesCalced[i].Depot.Id : -1,
                    //CreatorAdminId = routeRequest.CreatorAdminId // Need to add this field in the RouteRequest
                };

                // Add the new route to the database and save it to generate the ID
                scopedContext.DeliveryRoutes.Add(newRoute);
                await scopedContext.SaveChangesAsync();
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
                await scopedContext.SaveChangesAsync();
            }

            Console.WriteLine("Routes and orders updated successfully.");

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



        // GET: api/Calculation/{id}
        // Retrieves the status of a specific calculation by ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Calculation>> GetCalculationById(string id)
        {
            if (_context.Calculations == null)
            {
                return NotFound();
            }

            var calculation = await _context.Calculations.FindAsync(id);

            if (calculation == null)
            {
                return NotFound();
            }

            return calculation;
        }

        // PUT: api/Calculation/{id}
        // Updates the specified calculation object
       /* [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCalculation(string id, Calculation updatedCalculation)
        {
            if (id != updatedCalculation.ID)
            {
                return BadRequest();
            }

            var existingCalculation = await _context.Calculations.FindAsync(id);
            if (existingCalculation == null)
            {
                return NotFound();
            }

            // Update the properties as needed
            existingCalculation.Status = updatedCalculation.Status;
            existingCalculation.EndTime = updatedCalculation.EndTime; 
            // No other properties should change

            // Save changes to the database
            _context.Entry(existingCalculation).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent(); // Return 204 No Content
        }*/
    }
}


