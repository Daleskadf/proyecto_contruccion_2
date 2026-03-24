using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IFCMService
    {
        /// <summary>
        /// Envía una notificación push FCM a un token específico
        /// </summary>
        /// <param name="token">Token FCM del dispositivo</param>
        /// <param name="title">Título de la notificación</param>
        /// <param name="body">Contenido de la notificación</param>
        /// <param name="data">Datos adicionales (opcional)</param>
        /// <returns></returns>
        Task<bool> EnviarNotificacionAsync(string token, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Envía notificación push a múltiples tokens FCM
        /// </summary>
        /// <param name="tokens">Lista de tokens FCM</param>
        /// <param name="title">Título de la notificación</param>
        /// <param name="body">Contenido de la notificación</param>
        /// <param name="data">Datos adicionales (opcional)</param>
        /// <returns>Número de notificaciones enviadas exitosamente</returns>
        Task<int> EnviarNotificacionMultipleAsync(IEnumerable<string> tokens, string title, string body, Dictionary<string, string>? data = null);

        /// <summary>
        /// Envía notificación de alerta específicamente para patrulleros
        /// </summary>
        /// <param name="alertaData">Datos de la alerta</param>
        /// <returns></returns>
        Task<bool> EnviarNotificacionAlertaAsync(object alertaData);
    }
}