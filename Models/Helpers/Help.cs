namespace Weather.Models.Helpers
{
    public class WeatherForecastResponse
    {
        public Cit City { get; set; }
        public List<WeatherForecastItem> List { get; set; }
    }

    public class Cit
    {
        public string Name { get; set; }
    }

    public class WeatherForecastItem
    {
        public long Dt { get; set; }
        public Main Main { get; set; }
        public List<WeatherItem> Weather { get; set; } // Change type to List<WeatherItem>
    }

    public class WeatherItem
    {
        public string Description { get; set; }
        public string Main { get; set; }
    }

    public class Main
    {
        public double Temp { get; set; }
    }

    public class Response
    {
        public DateTime DateTime { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public double Temperature { get; set; }
        public string Commentary { get; set; }
    }


    public class Res
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}
