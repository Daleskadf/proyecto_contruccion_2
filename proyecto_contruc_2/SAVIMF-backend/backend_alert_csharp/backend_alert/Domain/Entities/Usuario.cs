namespace Domain.Entities
{
    public class Usuario
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public string Dni { get; set; } // 🆕 Nuevo campo
        public string Nombre { get; set; } // 🆕 Nuevo campo
        public string Apellido { get; set; } // 🆕 Para nombre completo
        public string Role { get; set; }
        public DateTime FechaRegistro { get; set; } // 🆕 Nuevo campo
        public bool EmailVerified { get; set; } // 🆕 Nuevo campo
        public string Estado { get; set; } // 🆕 Nuevo campo
        public string? FcmToken { get; set; } // 🆕 Token FCM para notificaciones push
        public DateTime? UltimaConexion { get; set; } // 🆕 Para saber si está activo

        public Usuario(string uid, string email, string dni, string nombre, string role)
        {
            Uid = uid;
            Email = email;
            Dni = dni;
            Nombre = nombre;
            Apellido = string.Empty;
            Role = role;
            FechaRegistro = DateTime.UtcNow;
            EmailVerified = false;
            Estado = "activo";
            FcmToken = null;
            UltimaConexion = DateTime.UtcNow;
        }

        // Constructor para lectura completa desde BD
        public Usuario(string uid, string email, string dni, string nombre, string apellido, string role,
                      DateTime fechaRegistro, bool emailVerified, string estado, string? fcmToken = null, DateTime? ultimaConexion = null)
        {
            Uid = uid;
            Email = email;
            Dni = dni;
            Nombre = nombre;
            Apellido = apellido ?? string.Empty;
            Role = role;
            FechaRegistro = fechaRegistro;
            EmailVerified = emailVerified;
            Estado = estado;
            FcmToken = fcmToken;
            UltimaConexion = ultimaConexion;
        }

        // Constructor vacío para deserialización
        public Usuario()
        {
            Uid = string.Empty;
            Email = string.Empty;
            Dni = string.Empty;
            Nombre = string.Empty;
            Apellido = string.Empty;
            Role = string.Empty;
            Estado = string.Empty;
            FcmToken = null;
        }

        // Puedes agregar lógica como:
        public bool EsOperador() => Role == "operador";
        public bool EsPatrulla() => Role == "patrullero";
        public bool PuedeRecibirNotificacionesFCM() => !string.IsNullOrEmpty(FcmToken) && Estado == "activo";
        public bool EstaConectadoRecientemente(int minutosLimite = 30) =>
            UltimaConexion.HasValue && (DateTime.UtcNow - UltimaConexion.Value).TotalMinutes <= minutosLimite;
    }
}