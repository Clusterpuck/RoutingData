using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoutingData.Controllers
{
    // to be deleted
    [ApiController]
    [Route("[controller]")]
    public class QuantumFactsController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        [HttpGet(Name = "GetQuantumFacts")]
        public string Get()
        {
            return Summaries[Random.Shared.Next(Summaries.Length)];
        }
    }
}