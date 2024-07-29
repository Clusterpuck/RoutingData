using Microsoft.AspNetCore.Mvc;

namespace RoutingData.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class QuantumFactsController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;

        public QuantumFactsController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetQuantumFacts")]
        public string Get()
        {
            return Summaries[Random.Shared.Next(Summaries.Length)];
        }
    }
}