using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json; // Required for JSON deserialization
using System;
using Weather.Infrastructure.Persistence;
using Weather.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Weather.Models;

namespace Weather.Controllers
{
    [ApiController]
    [Route("weather")]
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
                // Check if the city already exists in the database
                var existingCity = await _context.Cities.FirstOrDefaultAsync(c => c.Name == city);
                if (existingCity != null)
                {
                    return Ok("City exists in the database"); 
                }

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
                    

                    // Insert the City object into the database
                    _context.Cities.Add(cityObj);
                    await _context.SaveChangesAsync();

                    return Ok("City Inserted"); // Return 200 OK if insertion is successful
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while inserting city data into the database.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("{date}/{city}")]
        public async Task<IActionResult> InsertInDatabasePerDateAndCity(string city, DateTime date)
        {
            var client = new HttpClient();
            var coord = await _context.Cities.FirstOrDefaultAsync(m => m.Name == city);
            if (coord == null)
            {
               var res = await client.GetAsync($"http://127.0.0.1:5000//weather/coordinates/{city}");
               if(!res.IsSuccessStatusCode)
                {
                    throw new Exception("Something's wrong");
                }
               else coord = await _context.Cities.FirstOrDefaultAsync(m => m.Name == city);
            }
            
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://open-weather13.p.rapidapi.com/city/fivedaysforcast/{coord.latitude}/{coord.longitude}"),
                Headers =
                {
                    { "X-RapidAPI-Key", "f95ce684c2msh5ca0330a7bc3f70p115564jsnfc599ba663d2" },
                    { "X-RapidAPI-Host", "open-weather13.p.rapidapi.com" },
                },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                var weatherData = JsonConvert.DeserializeObject<WeatherForecastResponse>(body);
                var data = new List<WeatherTable>();
                foreach (var weather in weatherData.List)
                {
                    foreach (var desc in weather.Weather)
                    {
                        var City = weatherData.City.Name;
                        var Date = weather.Dt;
                        var Temp = weather.Main.Temp;
                        var Desc = desc.Description;
                        var Cond = desc.Main;

                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(Date);
                        DateTime dateTime = dateTimeOffset.UtcDateTime;

                        var existingWeather = await _context.Weather.FirstOrDefaultAsync(m => m.City == city && m.Date == date);
                        if (existingWeather == null)
                        {
                            var weatr = new WeatherTable
                            {
                                Id = new Guid(),
                                City = City,
                                Date = dateTime,
                                Temperature = Temp,
                                Description = Desc,
                                Condition = Cond
                            };
                            _context.Weather.Add(weatr);

                        }

                        await _context.SaveChangesAsync();

                    }


                }
                return Ok(body);
            }
        }

        [HttpGet]
        [Route("choose/{city}/{date}")]
        public async Task<IActionResult> GetFittingWeatherConditions(string city, DateTime date)
        {
            var inputDate = date.Date;

            // Query the database to find matching records
            WeatherTable coord = await _context.Weather.FirstOrDefaultAsync(m =>
                    m.City == city &&
                    m.Date.Date == inputDate && m.Date.TimeOfDay == TimeSpan.FromHours(15)

                    );
            var commentary = "";
            string time = coord.Date.TimeOfDay.ToString();
            if (coord != null)
            {
                
                if (coord.Condition.ToLower() == "rain" || coord.Condition.ToLower() == "thunderstorm" || coord.Condition.ToLower() == "snow" || coord.Condition.ToLower() == "tornado")
                {
                    commentary = commentary + $"Unfitting weather for a match.The meteorolgists predict {coord.Condition}, more exactly {coord.Description}.";
                    if (coord.Temperature < 278.15)
                    {
                        commentary = commentary + $" It may be too cold for this match at {coord.Temperature - 273.00} degrees Celsius. ";
                    }
                    commentary = commentary + " We suggest rescheduling.";
                }
                else commentary = $"Perfect weather for a match. Meterologists predict {coord.Condition}, more exactly {coord.Description}. The temperature will be {coord.Temperature - 273.00} degrees Celsius.";

            }
            else throw new Exception("Couldn't get details");
            return Ok(commentary);



        }



    }
}
