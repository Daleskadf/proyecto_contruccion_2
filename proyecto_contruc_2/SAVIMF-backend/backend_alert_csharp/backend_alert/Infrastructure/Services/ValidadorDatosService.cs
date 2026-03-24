using Domain.Entities;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    /// <summary>
    /// Servicio de validación de datos del dominio.
    /// Implementa el patrón Strategy para diferentes tipos de validaciones.
    /// </summary>
    public class ValidadorDatosService
    {
        private readonly ILogger<ValidadorDatosService> _logger;

        public ValidadorDatosService(ILogger<ValidadorDatosService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Valida los datos de una alerta LoRaWAN.
        /// </summary>
        public bool ValidarDatosAlerta(Alerta alerta)
        {
            return !string.IsNullOrEmpty(alerta.DevEUI)
                && alerta.Lat != 0
                && alerta.Lon != 0
                && alerta.Bateria != 0;
        }

        /// <summary>
        /// Valida el formato y estructura de un token FCM.
        /// Implementa validaciones específicas de Firebase Cloud Messaging.
        /// </summary>
        /// <param name="tokenFcm">Token FCM a validar</param>
        /// <returns>true si el token es válido, false en caso contrario</returns>
        public bool EsTokenFcmValido(string tokenFcm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tokenFcm))
                {
                    _logger.LogDebug("Token FCM nulo o vacío");
                    return false;
                }

                var token = tokenFcm.Trim();

                // Validación de longitud (tokens FCM típicamente entre 140-200 caracteres)
                if (token.Length < 20 || token.Length > 4096)
                {
                    _logger.LogDebug("Token FCM con longitud inválida: {Length}", token.Length);
                    return false;
                }

                // Validación de formato: FCM tokens son alfanuméricos con algunos caracteres especiales
                var formatoValido = Regex.IsMatch(token, @"^[a-zA-Z0-9_:.-]+$");
                if (!formatoValido)
                {
                    _logger.LogDebug("Token FCM con formato inválido");
                    return false;
                }

                // Validaciones adicionales específicas de FCM
                // Los tokens FCM modernos tienen ciertos patrones reconocibles

                // No debe ser solo números o letras
                if (Regex.IsMatch(token, @"^[0-9]+$") || Regex.IsMatch(token, @"^[a-zA-Z]+$"))
                {
                    _logger.LogDebug("Token FCM con patrón demasiado simple");
                    return false;
                }

                // Debe tener cierta diversidad de caracteres
                var caracteresUnicos = token.Distinct().Count();
                if (caracteresUnicos < 10)
                {
                    _logger.LogDebug("Token FCM con poca diversidad de caracteres: {UniqueChars}", caracteresUnicos);
                    return false;
                }

                _logger.LogDebug("Token FCM válido, longitud: {Length}, caracteres únicos: {UniqueChars}",
                    token.Length, caracteresUnicos);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar token FCM");
                return false;
            }
        }

        /// <summary>
        /// Valida si un usuario puede recibir notificaciones FCM según sus propiedades.
        /// Implementa reglas de negocio específicas del dominio.
        /// </summary>
        /// <param name="usuario">Usuario a validar</param>
        /// <returns>true si puede recibir notificaciones, false en caso contrario</returns>
        public bool PuedeRecibirNotificacionesFcm(Usuario usuario)
        {
            if (usuario == null)
            {
                _logger.LogDebug("Usuario nulo no puede recibir notificaciones FCM");
                return false;
            }

            // El usuario debe estar activo
            if (usuario.Estado?.ToLower() != "activo")
            {
                _logger.LogDebug("Usuario {Uid} no activo: {Estado}", usuario.Uid, usuario.Estado);
                return false;
            }

            // Roles permitidos para notificaciones FCM
            var rolesPermitidos = new[] { "patrullero", "operador", "victima" };
            if (!rolesPermitidos.Contains(usuario.Role?.ToLower()))
            {
                _logger.LogDebug("Role {Role} no permitido para notificaciones FCM", usuario.Role);
                return false;
            }

            // Debe tener un token FCM válido
            if (!EsTokenFcmValido(usuario.FcmToken ?? string.Empty))
            {
                _logger.LogDebug("Usuario {Uid} no tiene token FCM válido", usuario.Uid);
                return false;
            }

            _logger.LogDebug("Usuario {Uid} puede recibir notificaciones FCM", usuario.Uid);
            return true;
        }

        /// <summary>
        /// Valida coordenadas GPS para alertas de emergencia.
        /// </summary>
        /// <param name="latitud">Latitud en grados decimales</param>
        /// <param name="longitud">Longitud en grados decimales</param>
        /// <returns>true si las coordenadas son válidas</returns>
        public bool ValidarCoordenadas(double latitud, double longitud)
        {
            // Validar rangos de coordenadas válidas
            var latitudValida = latitud >= -90.0 && latitud <= 90.0 && latitud != 0.0;
            var longitudValida = longitud >= -180.0 && longitud <= 180.0 && longitud != 0.0;

            return latitudValida && longitudValida;
        }

        /// <summary>
        /// Valida el nivel de batería de un dispositivo LoRaWAN.
        /// </summary>
        /// <param name="nivelBateria">Nivel de batería (0-100)</param>
        /// <returns>true si el nivel es válido</returns>
        public bool ValidarNivelBateria(int nivelBateria)
        {
            return nivelBateria >= 0 && nivelBateria <= 100;
        }
    }
}