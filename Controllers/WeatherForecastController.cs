using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Newtonsoft.Json; // Required for JSON deserialization
using System;
using Weather.Infrastructure.Persistence;
using Weather.Models.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Weather.Models;
using MySqlX.XDevAPI;
using System.Drawing.Drawing2D;

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
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

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
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
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
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
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
                    RequestUri = new Uri($"https://open-weather13.p.rapidapi.com/city/{city}"),
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
                        Name = city,
                        longitude = weatherData.Coord.lon,
                        latitude = weatherData.Coord.lat
                    };
                    

                    // Insert the City object into the database
                    _context.Cities.Add(cityObj);
                    await _context.SaveChangesAsync();

                    return Ok($"{cityObj.Name} with coordinates: latitude {cityObj.latitude} and longitude {cityObj.longitude} "); // Return 200 OK if insertion is successful
                }
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while inserting city data into the database.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        [Route("forecast/{city}")]
        public async Task<IActionResult> InsertInDatabasePerDateAndCity(string city)
        {

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            try
            {
                var client = new HttpClient();
                var coord = await _context.Cities.FirstOrDefaultAsync(m => m.Name == city);
                if (coord == null)
                {
                    var res = await client.GetAsync($"http://gateway:5000/weather/coordinates/{city}");
                    if (!res.IsSuccessStatusCode)
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
                            data.Add(weatr);


                            await _context.SaveChangesAsync();

                        }
                    }
                    return Ok(data);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }



        }

      
        [HttpGet]
        [Route("choose/{city}/{date}")]
        public async Task<IActionResult> GetFittingWeatherConditions(string city, DateTime date)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                var inputDate = date.Date;
                var st = inputDate.ToString();
                var tab = st.Split();

                // Query the database to find matching records
                WeatherTable coord = await _context.Weather.FirstOrDefaultAsync(m =>
                        m.City == city &&
                        m.Date.Date == inputDate
               );
                var commentary = $"The match is scheduled on {tab[0]} in {city.ToUpper()}. ";

                if (coord != null)
                {

                    if ((coord.Condition.ToLower() == "rain" || coord.Condition.ToLower() == "thunderstorm" || coord.Condition.ToLower() == "snow" || coord.Condition.ToLower() == "tornado") && coord.Temperature < 278.15)
                    {
                        commentary = commentary + $"Unfitting weather for a match .The meteorologists predict {coord.Condition}, more exactly {coord.Description}. It may be too cold for this match at {coord.Temperature - 273.00} degrees Celsius.";
                    }
                    else if (coord.Condition.ToLower() == "rain" || coord.Condition.ToLower() == "thunderstorm" || coord.Condition.ToLower() == "snow" || coord.Condition.ToLower() == "tornado")
                    {
                        commentary = commentary + $"Unfitting weather for a match.The meteorologists predict {coord.Condition}, more exactly {coord.Description}.";
                    }
                    else if (coord.Temperature < 278.15)
                    {
                        commentary = commentary + $"It may be too cold for this match at {coord.Temperature - 273.00} degrees Celsius.";
                    }
                    else
                    {
                        commentary = commentary + $"Perfect weather for a match. Meterologists predict {coord.Condition}, more exactly {coord.Description}. The temperature will be {coord.Temperature - 273.00} degrees Celsius.";
                    }

                }
                else throw new Exception("Couldn't get details");
                return Ok(commentary);
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
        }


        [HttpGet]
        [Route("{city}")]
        public async Task<IActionResult> GetWeatherRightNow(string city)

        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                var client = new HttpClient();
                var coord = await _context.Cities.FirstOrDefaultAsync(m => m.Name == city);
                if (coord == null)
                {
                    var res = await client.GetAsync($"http://gateway:5000/weather/coordinates/{city}");
                    if (!res.IsSuccessStatusCode)
                    {
                        throw new Exception("Something's wrong");
                    }
                    else coord = await _context.Cities.FirstOrDefaultAsync(m => m.Name == city);
                }

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://open-weather13.p.rapidapi.com/city/latlon/{coord.latitude}/{coord.longitude}"),
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
                    var weatherData = JsonConvert.DeserializeObject<WeatherForecastItem>(body);
                    var data = new List<WeatherTable>();

                    foreach (var desc in weatherData.Weather)
                    {
                        var City = city;
                        var Date = DateTime.Now;
                        var Temp = weatherData.Main.Temp;
                        var Desc = desc.Description;
                        var Cond = desc.Main;




                        var weatr = new WeatherTable
                        {
                            Id = new Guid(),
                            City = City,
                            Date = Date,
                            Temperature = Temp,
                            Description = Desc,
                            Condition = Cond
                        };
                        _context.Weather.Add(weatr);
                        data.Add(weatr);



                        await _context.SaveChangesAsync();

                    }
                    return Ok(data);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }

        }

        [HttpGet]
        [Route("warmestday/{city}")]
        public async Task<IActionResult> GetWarmestWeather(string city)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                var client = new HttpClient();
                var days = await _context.Weather.Where(m => m.City == city).ToListAsync();

                if (days.Count == 0)
                {
                    var res = await client.GetAsync($"http://gateway:5000/weather/coordinates/{city}");
                    if (!res.IsSuccessStatusCode)
                    {
                        throw new Exception("Something's wrong");
                    }
                    else
                    {
                        days = await _context.Weather.Where(m => m.City == city).ToListAsync();
                    }
                }

                // Initialize variables to keep track of the warmest day and its temperature
                DateTime warmestDay = DateTime.MinValue;
                double maxTemperature = double.MinValue;
                string response = "";


                // Iterate through each day in the list
                foreach (var day in days)
                {
                    // Check if the day is within the next 5 days
                    if (day.Date >= DateTime.Today && day.Date <= DateTime.Today.AddDays(5))
                    {
                        // Check if the condition is not rain or thunderstorm
                        if (day.Condition.ToLower() != "rain" && day.Condition.ToLower() != "thunderstorm")
                        {
                            
                            // If the condition is sunny, choose that day regardless of temperature
                            if (day.Condition.ToLower() == "sunny")

                            { 
                                warmestDay = day.Date;
                                response += $"Choose {warmestDay.Date} since it is predicted to be sunny";
                                break; // Exit the loop since we found a sunny day
                            }
                            else
                            {
                                // If the temperature is higher than the current maximum, update the warmest day
                                if (day.Temperature > maxTemperature)
                                {
                                    maxTemperature = day.Temperature;
                                    warmestDay = day.Date;
                                }
                            }
                        }
                       
                    }
                }
                if (warmestDay == DateTime.MinValue) response += "There is no day without rain in the next 5 days. We suggest waitng a couple of days";
                if (response.Length == 0) response += $"Choose {warmestDay.Date} since it is the warmest day in the next 5 days.";
                var resp = new Res
                {
                    Date = warmestDay.Date,
                    Description = response
                };

                // Return the warmest day found
                return Ok(resp);
            }
            catch (OperationCanceledException)
            {
                // Handle the timeout
                return StatusCode(504, "Request timed out");
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                _logger.LogError(ex, "An error occurred while retrieving the warmest weather.");
                return StatusCode(500, "An error occurred while retrieving the warmest weather.");
            }
        }

      


    }

}
