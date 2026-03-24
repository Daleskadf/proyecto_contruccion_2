namespace WebAPI.Controllers.Models
{
    public class EditarUsuarioRequestDto
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
    }
}