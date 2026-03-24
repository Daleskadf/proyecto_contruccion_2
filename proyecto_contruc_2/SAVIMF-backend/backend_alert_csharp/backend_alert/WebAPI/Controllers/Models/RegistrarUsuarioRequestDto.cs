namespace WebAPI.Controllers.Models
{
    public class RegistrarUsuarioRequestDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Role { get; set; } = ""; // "patrullero" o "operador"
    }

    public class RegistrarUsuarioResponseDto
    {
        public string Uid { get; set; } = "";
        public string Email { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Role { get; set; } = "";
        public string Mensaje { get; set; } = "";
    }
}