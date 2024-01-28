namespace Weather
{
    public class City
    {
        public Guid Id = new Guid();
        public string Name { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }

    }
}
