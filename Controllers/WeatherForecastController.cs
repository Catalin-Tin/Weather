using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json; // Required for JSON deserialization
using System;
using Weather.Infrastructure.Persistence;
using Weather.Models.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Weather.Controllers
{
    [ApiController]
    [Route("api")]
    public class WeatherForecastController : ControllerBase
    {
        

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ApplicationDbContext _context;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        [HttpGet]
        [Route("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Attempt to retrieve the city from the database
                var city = await _context.Cities.FirstOrDefaultAsync(c => c.Name == "Landon");

                if (city == null)
                {
                    // City with the specified Id not found
                    return Ok("Microservice is functional");
                }

                // City found, return it with status OK
                return Ok("Microservice is functional");
            }
            catch (Exception ex)
            {
                // Log any errors that occur during database access
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
      


        [HttpGet]
        [Route("getCoord/{city}")]
        public async Task<IActionResult> InsertCityIntoDatabaseAsync(string city)
        {
            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://open-weather13.p.rapidapi.com/city/{Uri.EscapeUriString(city)}"),
                    Headers =
            {
                { "X-RapidAPI-Key", "f95ce684c2msh5ca0330a7bc3f70p115564jsnfc599ba663d2" },
                { "X-RapidAPI-Host", "open-weather13.p.rapidapi.com" },
            },
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response to extract coordinates
                    var weatherData = JsonConvert.DeserializeObject<WeatherData>(jsonResponse);

                    // Create a new City object with extracted coordinates
                    var cityObj = new City
                    {
                        Id = Guid.NewGuid(), // Generate a new GUID for the city
                        Name = weatherData.Name,
                        longitude = weatherData.Coord.lon,
                        latitude = weatherData.Coord.lat
                    };
                    _logger.Log(LogLevel.Information, cityObj.Name.ToString());

                    // Insert the City object into the database
                    _context.Cities.Add(cityObj);
                    await _context.SaveChangesAsync();

                    return Ok(); // Return 200 OK if insertion is successful
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while inserting city data into the database.");
                return StatusCode(500, ex.Message);
            }
        }

    }
}
