namespace WebAPI.Models
{
    public class CambiarEstadoRequestDto
    {
        public string alertaId { get; set; } = string.Empty;
        public string patrulleroId { get; set; } = string.Empty;
        public string nuevoEstado { get; set; } = string.Empty;
    }
}