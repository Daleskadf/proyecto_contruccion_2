using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace WebAPI.Controllers.Models
{
    /// <summary>
    /// DTO para el registro de token FCM de usuarios.
    /// Implementa Data Transfer Object Pattern con validaciones integradas.
    /// Soporta múltiples nombres de campo para compatibilidad con diferentes clientes.
    /// </summary>
    public class RegistrarTokenFcmRequest
    {
        /// <summary>
        /// Token FCM generado por Firebase en la aplicación móvil.
        /// Acepta múltiples nombres de campo para flexibilidad de integración.
        /// </summary>
        [JsonPropertyName("fcmToken")]
        public string? FcmToken { get; set; }

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("registrationToken")]
        public string? RegistrationToken { get; set; }

        [JsonPropertyName("deviceToken")]
        public string? DeviceToken { get; set; }

        /// <summary>
        /// Token FCM unificado extraído de cualquiera de los campos anteriores.
        /// Implementa el patrón Adapter para normalizar diferentes formatos de entrada.
        /// </summary>
        [JsonIgnore]
        public string TokenFcmFinal =>
            !string.IsNullOrWhiteSpace(FcmToken) ? FcmToken :
            !string.IsNullOrWhiteSpace(Token) ? Token :
            !string.IsNullOrWhiteSpace(RegistrationToken) ? RegistrationToken :
            !string.IsNullOrWhiteSpace(DeviceToken) ? DeviceToken :
            string.Empty;

        /// <summary>
        /// Información adicional del dispositivo (opcional).
        /// Útil para análisis y debugging de la entrega de notificaciones.
        /// </summary>
        [JsonPropertyName("deviceInfo")]
        public DeviceInfoDto? DeviceInfo { get; set; }

        /// <summary>
        /// Timestamp de cuando se generó el token en el cliente.
        /// Útil para análisis de rendimiento y debugging.
        /// </summary>
        [JsonPropertyName("clientTimestamp")]
        public DateTime? ClientTimestamp { get; set; }

        /// <summary>
        /// Valida que el DTO contenga datos válidos.
        /// Implementa el patrón Specification para validaciones complejas.
        /// </summary>
        public (bool EsValido, string[] Errores) ValidarDTO()
        {
            var errores = new List<string>();

            // Validar que al menos uno de los campos de token esté presente
            if (string.IsNullOrWhiteSpace(TokenFcmFinal))
            {
                errores.Add("Token FCM es requerido. Proporcione uno de: fcmToken, token, registrationToken, deviceToken");
            }
            else
            {
                // Validaciones específicas del token FCM
                var token = TokenFcmFinal.Trim();

                if (token.Length < 20)
                    errores.Add("Token FCM muy corto. Los tokens de Firebase típicamente tienen más de 20 caracteres");

                if (token.Length > 4096)
                    errores.Add("Token FCM muy largo. Límite máximo: 4096 caracteres");

                // Validar caracteres permitidos (Base64 + algunos símbolos especiales de FCM)
                if (!System.Text.RegularExpressions.Regex.IsMatch(token, @"^[a-zA-Z0-9_:.-]+$"))
                    errores.Add("Token FCM contiene caracteres inválidos");
            }

            return (errores.Count == 0, errores.ToArray());
        }

        /// <summary>
        /// Obtiene información de debugging del DTO para logging.
        /// </summary>
        public object GetDebugInfo()
        {
            return new
            {
                TieneFcmToken = !string.IsNullOrWhiteSpace(FcmToken),
                TieneToken = !string.IsNullOrWhiteSpace(Token),
                TieneRegistrationToken = !string.IsNullOrWhiteSpace(RegistrationToken),
                TieneDeviceToken = !string.IsNullOrWhiteSpace(DeviceToken),
                TokenFinalLength = TokenFcmFinal?.Length ?? 0,
                TieneDeviceInfo = DeviceInfo != null,
                ClientTimestamp = ClientTimestamp,
                ServerTimestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Información adicional del dispositivo para análisis y debugging.
    /// </summary>
    public class DeviceInfoDto
    {
        [JsonPropertyName("platform")]
        public string? Platform { get; set; } // "android", "ios"

        [JsonPropertyName("appVersion")]
        public string? AppVersion { get; set; }

        [JsonPropertyName("deviceModel")]
        public string? DeviceModel { get; set; }

        [JsonPropertyName("osVersion")]
        public string? OsVersion { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }
    }

    /// <summary>
    /// DTO de respuesta para el registro de token FCM.
    /// Implementa Response Object Pattern con información detallada.
    /// </summary>
    public class RegistrarTokenFcmResponse
    {
        [JsonPropertyName("exitoso")]
        public bool Exitoso { get; set; }

        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [JsonPropertyName("errores")]
        public string[]? Errores { get; set; }

        [JsonPropertyName("usuario")]
        public UsuarioTokenFcmDto? Usuario { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("tokenRegistrado")]
        public bool TokenRegistrado { get; set; }

        public static RegistrarTokenFcmResponse Exito(string uid, string nombre, string role, string email)
        {
            return new RegistrarTokenFcmResponse
            {
                Exitoso = true,
                Mensaje = "Token FCM registrado exitosamente",
                TokenRegistrado = true,
                Usuario = new UsuarioTokenFcmDto
                {
                    Uid = uid,
                    Nombre = nombre,
                    Role = role,
                    Email = email
                }
            };
        }

        public static RegistrarTokenFcmResponse Fallo(string[] errores)
        {
            return new RegistrarTokenFcmResponse
            {
                Exitoso = false,
                Mensaje = "Error al registrar token FCM",
                Errores = errores,
                TokenRegistrado = false
            };
        }
    }

    /// <summary>
    /// DTO con información mínima del usuario para la respuesta.
    /// Implementa el principio de mínima exposición de datos.
    /// </summary>
    public class UsuarioTokenFcmDto
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}