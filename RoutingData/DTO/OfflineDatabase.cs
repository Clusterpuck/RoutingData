using RoutingData.Models;

namespace RoutingData.DTO
{
    public class OfflineDatabase
    {
        public OfflineDatabase() 
        {
            BuildCustomers();
            BuildLocations();
            BuildProducts();
            BuildOrders();
            BuildProductOrders();
            BuildDrivers();
            BuildVehicles();
            BuildAdminAccounts();
            deliveryRoutes = new List<DeliveryRoute>();
        }
        public List<Customer> Customers { get; set; }
        public List<Location> Locations { get; set; }
        public List<Order> Orders { get; set; }
        public List<Product> Products { get; set; }
        public List<OrderProduct> OrderProducts { get; set; }
        public List<DeliveryRoute> deliveryRoutes { get; set; }
        public List<Driver> Drivers { get; set; }
        public List<Vehicle> Vehicles { get; set; }
        public List<AdminAccount> AdminAccounts { get; set; }   

        private void BuildCustomers()
        {
            Customers = new List<Customer>();
            for (int i = 1; i < 10; i++)

            {
                Customer customer = new Customer();
                customer.Name = "Test Customer " + i;
                customer.Phone = $"{i:D10}";
                customer.Id = i;
                Customers.Add(customer);
            }
        }

        private void BuildAdminAccounts()
        {
            AdminAccounts = new List<AdminAccount>
            {
                new AdminAccount { Username = "admin1", Password = "password1" },
                new AdminAccount { Username = "admin2", Password = "password2" },
                new AdminAccount { Username = "admin3", Password = "password3" },
            };
        }

        private void BuildVehicles()
        {
            Vehicles = new List<Vehicle>
            {
                new Vehicle { Id = 1, LicensePlate = "Terminator" },
                new Vehicle { Id = 2, LicensePlate = "RoadRunner" },
                new Vehicle { Id = 3, LicensePlate = "Thunderbolt" },
                new Vehicle { Id = 4, LicensePlate = "SilverBullet" },
                new Vehicle { Id = 5, LicensePlate = "Falcon" },
                new Vehicle { Id = 6, LicensePlate = "Lightning" },
            };
        }

        private void BuildDrivers()
        {
            Drivers = new List<Driver>
            {
                new Driver { Username = "Bob1", Name = "Bob", Phone = "555 123 456", Password = "password123" },
                new Driver { Username = "Alice1", Name = "Alice", Phone = "555 234 567", Password = "password456" },
                new Driver { Username = "Charlie1", Name = "Charlie", Phone = "555 345 678", Password = "password789" },
                new Driver { Username = "Diana1", Name = "Diana", Phone = "555 456 789", Password = "password101" },
                new Driver { Username = "Eve1", Name = "Eve", Phone = "555 567 890", Password = "password102" },
                new Driver { Username = "Frank1", Name = "Frank", Phone = "555 678 901", Password = "password103" },
            };

        }

        private void BuildLocations()
        {
            Locations = new List<Location>
            {
                new Location { Id = 1, Longitude = 115.8146751, Latitude = -32.1375223, Address = "42 Wallaby Way", Suburb = "Bertram", State = "WA", Country = "Australia", PostCode = 6167, Description = "Fake Address" },
                new Location { Id = 2, Longitude = 115.8146732, Latitude = -32.1375187, Address = "21 Peregrine Circle", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 3, Longitude = 115.8130603, Latitude = -32.1404435, Address = "66 Mannikin Heights", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 4, Longitude = 115.79835, Latitude = -32.13816, Address = "128 Fanstone Avenue", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 5, Longitude = 115.79112, Latitude = -32.13816, Address = "54 Fanstone Avenue", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 6, Longitude = 115.80386, Latitude = -32.15274, Address = "32 Holmes Road", Suburb = "Munster", State = "Western Australia", Country = "Australia", PostCode = 6166, Description = "No Description" },
                new Location { Id = 7, Longitude = 115.8130603, Latitude = -32.1404435, Address = "66 Mannikin Heights", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 8, Longitude = 115.799275, Latitude = -32.133987, Address = "143 East Churchill Avenue", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 9, Longitude = 115.8084911, Latitude = -32.135661, Address = "143 Tindal Avenue", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 10, Longitude = 115.8130603, Latitude = -32.1404435, Address = "66 Mannikin Heights", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 11, Longitude = 115.8145594, Latitude = -32.1386723, Address = "43 Peregrine Circle", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 12, Longitude = 115.810722, Latitude = -32.136686, Address = "8 Retusa Street", Suburb = "Beeliar", State = "Western Australia", Country = "Australia", PostCode = 6164, Description = "No Description" },
                new Location { Id = 13, Longitude = 115.897796, Latitude = -31.984252, Address = "243b Gloucester Street", Suburb = "East Victoria Park", State = "Western Australia", Country = "Australia", PostCode = 6101, Description = "No Description" }
            };

        }

        private void BuildProducts()
        {
            Products = new List<Product>
            {
                new Product { Id = 1, Name = "Apples", UnitOfMeasure = "Kilograms" },
                new Product { Id = 2, Name = "Milk", UnitOfMeasure = "Liters" },
                new Product { Id = 3, Name = "Steel Beams", UnitOfMeasure = "Meters" },
                new Product { Id = 4, Name = "Nails", UnitOfMeasure = "Kilograms" },
                new Product { Id = 5, Name = "Pallets", UnitOfMeasure = "Pallets" }
            };
        }

        private void BuildOrders()
        {
            Orders = new List<Order> {
                   new Order
                {
                    Id = 1,
                    DateOrdered = DateTime.Now,
                    OrderNotes = "Order 1 Notes",
                    CustomerId = 1,
                    LocationId = 1,
                    DeliveryRouteId = 1,
                    PositionNumber = 1
                },
                new Order
                {
                    Id = 2,
                    DateOrdered = DateTime.Now.AddDays(-1),
                    OrderNotes = "Order 2 Notes",
                    CustomerId = 2,
                    LocationId = 2,
                    DeliveryRouteId = 1,
                    PositionNumber = 2
                },
                new Order
                {
                    Id = 3,
                    DateOrdered = DateTime.Now.AddDays(-2),
                    OrderNotes = "Order 3 Notes",
                    CustomerId = 3,
                    LocationId = 3,
                    DeliveryRouteId = 2,
                    PositionNumber = 3
                }
            };
        }

        public void BuildProductOrders()
        {
            OrderProducts = new List<OrderProduct>
            {
                new OrderProduct { OrderId = 1, ProductId = 1, Quantity = 2 },
                new OrderProduct { OrderId = 1, ProductId = 2, Quantity = 1 },
                new OrderProduct { OrderId = 2, ProductId = 3, Quantity = 4 },
                new OrderProduct { OrderId = 3, ProductId = 4, Quantity = 3 },
                new OrderProduct { OrderId = 3, ProductId = 5, Quantity = 5 }
                // Additional OrderProduct objects...
            };
        }

        public Task<List<AdminAccount>> GetAdminAccountsAsync()
        {
            return Task.FromResult(AdminAccounts);
        }

        public Task<AdminAccount> FindAdminAccountAsync(string username)
        {
            return Task.FromResult(AdminAccounts.FirstOrDefault(a => a.Username == username));
        }

        public Task AddAdminAccountAsync(AdminAccount adminAccount)
        {
            AdminAccounts.Add(adminAccount);
            return Task.CompletedTask;
        }

        public Task RemoveAdminAccountAsync(AdminAccount adminAccount)
        {
            AdminAccounts.Remove(adminAccount);
            return Task.CompletedTask;
        }

        public void Entry(AdminAccount adminAccount)
        {
            // a placeholder for Entity Framework's Entry method to change state
        }

        //this is the offline version, will also need an online version. 
        public Dictionary<int, OrderDetail> MakeOrdersDictionary()
        {

            Dictionary<int, RoutingData.Models.Location> locationDict =
                Locations.ToDictionary(l => l.Id);
            Dictionary<int, Customer> customerDict =
                Customers.ToDictionary(c => c.Id);
            Dictionary<int, Product> productDict =
                Products.ToDictionary(p => p.Id);

            Dictionary<int, OrderDetail> orderDetailsDict =
                new Dictionary<int, OrderDetail>();

            foreach (Order order in Orders)
            {
                // Get the location, customer, and order products associated with this order
                RoutingData.Models.Location location = locationDict[order.LocationId];
                Customer customer = customerDict[order.CustomerId];

                // Get all products associated with this order
                var orderProductList =
                    OrderProducts.Where(op => op.OrderId == order.Id).ToList();
                List<string> productNames = new List<string>();

                foreach (var orderProduct in orderProductList)
                {
                    if (productDict.ContainsKey(orderProduct.ProductId))
                    {
                        productNames.Add(productDict[orderProduct.ProductId].Name);
                    }
                }

                // Create an OrderDetail object
                OrderDetail orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    Addr = location.Address,
                    Lat = location.Latitude,
                    Lon = location.Longitude,
                    Status = "Pending",
                    CustomerName = customer.Name,
                    Phone = customer.Phone,
                    ProdNames = productNames,
                    Position = order.PositionNumber,
                    DeliveryDate = order.DeliveryDate,
                    Notes = order.OrderNotes,
                };

                // Add the orderDetail to the Hashtable using the OrderId as the key
                orderDetailsDict.Add(order.Id, orderDetail);
            }
            return orderDetailsDict;
        }

    }
}
