using Microsoft.AspNetCore.Mvc;
using RoutingData.Controllers;
using RoutingData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRoutingData
{
    public class LocationTests
    {
        private readonly LocationsController _locationController;

        public LocationTests()
        {
            _locationController = new LocationsController(TestServiceProvider.OfflineDatabaseInstance);
        }

#if OFFLINE_DATA

        [Fact]
        public async Task Get_Locations()
        {
            // Act
            var result = await _locationController.GetLocations();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Location>>>(result);

            Assert.NotNull(actionResult.Value);

            var locations = Assert.IsAssignableFrom<IEnumerable<Location>>(actionResult.Value);

        }

        [Fact]

        public async Task Post_Location()
        {
            Location location = new Location
            {
                Longitude = 1.1,
                Latitude = 1.2,
                Address = "Test Address",
                Suburb = "Test Suburb",
                State = "Of Pain",
                Country = "AussieLand",
                Description = "This is just a test"
            };

            var result = await _locationController.PostLocation(location);

            var actionResult = Assert.IsType<ActionResult<Location>>(result);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);


            var returnedLocation = Assert.IsType<Location>(createdResult.Value);
            Assert.Equal(TestServiceProvider.OfflineDatabaseInstance.Locations.Last().Id, returnedLocation.Id);
            AssertLocationEqual(location, returnedLocation);

            var storedResult = await _locationController.GetLocations();
            var locationsList = Assert.IsType<List<Location>>(storedResult.Value);
            var storedLocation = locationsList.Last();

            Assert.Equal(location.Id, storedLocation.Id);
            AssertLocationEqual(location, storedLocation);
        }

        private void AssertLocationEqual(Location expected, Location actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.Equal(expected.Longitude, actual.Longitude);
            Assert.Equal(expected.Latitude, actual.Latitude);
            Assert.Equal(expected.Address, actual.Address);
            Assert.Equal(expected.Suburb, actual.Suburb);
            Assert.Equal(expected.State, actual.State);
            Assert.Equal(expected.Country, actual.Country);
            Assert.Equal(expected.Description, actual.Description);
        }


#endif
    }
}
