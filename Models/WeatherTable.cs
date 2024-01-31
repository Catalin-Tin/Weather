namespace Weather.Models
{
    public class WeatherTable
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string City { get; set; }
        public string Description { get; set; }
        public double Temperature {  get; set; }
        public string Condition { get; set; }

    }
}
