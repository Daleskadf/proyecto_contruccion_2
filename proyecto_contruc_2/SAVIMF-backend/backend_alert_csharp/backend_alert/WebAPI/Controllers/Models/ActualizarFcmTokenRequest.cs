using System.Text.Json.Serialization;

namespace WebAPI.Controllers.Models
{
    /// DTO para actualizar token FCM de notificaciones push
    public class ActualizarFcmTokenRequest
    {
        /// Token FCM del dispositivo para notificaciones push
        /// Acepta múltiples nombres de campo para compatibilidad
        [JsonPropertyName("fcmToken")]
        public string FcmToken { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("registrationToken")]
        public string? RegistrationToken { get; set; }

        [JsonIgnore]
        public string TokenFcmFinal =>
            !string.IsNullOrEmpty(FcmToken) ? FcmToken :
            !string.IsNullOrEmpty(Token) ? Token :
            !string.IsNullOrEmpty(RegistrationToken) ? RegistrationToken :
            string.Empty;
    }
}