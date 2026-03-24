namespace WebAPI.Models
{
    public class PatrullaUbicacionDto
    {
        public string PatrulleroId { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public DateTime Timestamp { get; set; }
        public string Estado { get; set; } = "Activa";
        public double MinutosDesdeUltimaActualizacion { get; set; }

        public PatrullaUbicacionDto(string patrulleroId, double lat, double lon, DateTime timestamp)
        {
            PatrulleroId = patrulleroId;
            Lat = lat;
            Lon = lon;
            Timestamp = timestamp;

            // Calcular estado basado en el tiempo transcurrido
            var tiempoTranscurrido = DateTime.UtcNow - timestamp;
            MinutosDesdeUltimaActualizacion = tiempoTranscurrido.TotalMinutes;
            Estado = MinutosDesdeUltimaActualizacion <= 10 ? "Activa" : "Inactiva";
        }
    }
}