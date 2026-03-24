// Puedes ponerlo en WebAPI/Models o en Domain/Entities según tu arquitectura
public class UsuarioFirebaseDto
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}