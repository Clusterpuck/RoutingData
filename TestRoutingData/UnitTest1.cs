using RoutingData.Controllers;
using System.Reflection;


namespace TestRoutingData
{
    public class QuantumFactsControllerTests
    {
        [Fact]
        public void Get_ReturnsValueFromSummaries()
        {
            // Arrange
            var controller = new QuantumFactsController(null);

            // Act
            var result = controller.Get();

            var fieldInfo = typeof(QuantumFactsController).GetField("Summaries", BindingFlags.NonPublic | BindingFlags.Static);
            var summaries = (string[])fieldInfo.GetValue(null);

            // Assert
            Assert.Contains(result, summaries);
        }
    }


}