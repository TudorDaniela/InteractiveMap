namespace InteractiveMap.Server
{
    public class Country
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string Capital { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public long Population { get; set; }

        public string[] Languages { get; set; } = [];

        public string Flag { get; set; } = string.Empty;
    }
}