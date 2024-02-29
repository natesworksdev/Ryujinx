namespace Ryujinx.Ava.UI.Models
{
    public class TimeZone
    {
        public TimeZone(string utcDifference, string location, string abbreviation)
        {
            UtcDifference = utcDifference;
            Location = location;
            Abbreviation = abbreviation;
        }

        public string ToString()
        {
            // Prettifies location strings eg:
            // "America/Costa_Rica" -> "Costa Rica"
            var location = Location.Replace("_", " ");

            if (location.Contains("/"))
            {
                var parts = location.Split("/");
                location = parts[1];
            }

            return $"{UtcDifference} - {location}";
        }

        public string UtcDifference { get; set; }
        public string Location { get; set; }
        public string Abbreviation { get; set; }
    }
}
