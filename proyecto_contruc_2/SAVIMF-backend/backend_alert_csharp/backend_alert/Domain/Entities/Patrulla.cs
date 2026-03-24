
namespace Domain.Entities
{
    public class Patrulla
    {
        public string PatrulleroId { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTime Timestamp { get; set; }

        public Patrulla(string patrulleroId, double lat, double lon, DateTime timestamp)
        {
            PatrulleroId = patrulleroId;
            Lat = lat;
            Lon = lon;
            Timestamp = timestamp;
        }
    }
}