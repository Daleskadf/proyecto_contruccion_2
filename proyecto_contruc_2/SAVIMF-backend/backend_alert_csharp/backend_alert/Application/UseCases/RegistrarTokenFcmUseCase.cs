using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Services;

namespace Application.UseCases
{
    /// <summary>
    /// Caso de uso para registrar token FCM de un usuario autenticado.
    /// Implementa el principio de responsabilidad única (SRP) del SOLID.
    /// </summary>
    public class RegistrarTokenFcmUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;
        private readonly ValidadorDatosService _validadorDatos;
        private readonly ILogger<RegistrarTokenFcmUseCase> _logger;

        public RegistrarTokenFcmUseCase(
            IUserRepositoryFirestore userRepository,
            ValidadorDatosService validadorDatos,
            ILogger<RegistrarTokenFcmUseCase> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _validadorDatos = validadorDatos ?? throw new ArgumentNullException(nameof(validadorDatos));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registra o actualiza el token FCM para un usuario específico.
        /// </summary>
        /// <param name="usuarioUid">UID del usuario autenticado</param>
        /// <param name="tokenFcm">Token FCM generado por la aplicación móvil</param>
        /// <returns>Resultado de la operación con información del usuario</returns>
        public async Task<RegistrarTokenFcmResult> EjecutarAsync(string usuarioUid, string tokenFcm)
        {
            _logger.LogInformation("Iniciando registro de token FCM para usuario: {UsuarioUid}", usuarioUid);

            // 🔐 Validaciones de entrada (Input Validation)
            var validacionEntrada = ValidarEntrada(usuarioUid, tokenFcm);
            if (!validacionEntrada.EsValido)
            {
                _logger.LogWarning("Validación de entrada falló: {Errores}", string.Join(", ", validacionEntrada.Errores));
                return RegistrarTokenFcmResult.Fallo(validacionEntrada.Errores);
            }

            try
            {
                // 🔍 Verificar que el usuario existe
                var usuarioDto = await _userRepository.BuscarPorUidAsync(usuarioUid);
                if (usuarioDto == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {UsuarioUid}", usuarioUid);
                    return RegistrarTokenFcmResult.Fallo(new[] { "Usuario no encontrado" });
                }

                // Convertir DTO a entidad para validaciones de negocio
                var usuario = new Usuario
                {
                    Uid = usuarioDto.Uid,
                    Email = usuarioDto.Email,
                    Nombre = usuarioDto.Nombre,
                    Role = usuarioDto.Role,
                    Estado = "activo" // Asumimos activo para usuarios existentes
                };

                // ✅ Validaciones de negocio
                var validacionNegocio = ValidarReglasSistema(usuario, tokenFcm);
                if (!validacionNegocio.EsValido)
                {
                    _logger.LogWarning("Validación de negocio falló para usuario {UsuarioUid}: {Errores}",
                        usuarioUid, string.Join(", ", validacionNegocio.Errores));
                    return RegistrarTokenFcmResult.Fallo(validacionNegocio.Errores);
                }

                // 🔄 Actualizar token FCM (idempotente)
                var exito = await _userRepository.ActualizarFcmTokenAsync(usuarioUid, tokenFcm);

                if (exito)
                {
                    _logger.LogInformation("Token FCM actualizado exitosamente para usuario {UsuarioUid} con role {Role}",
                        usuarioUid, usuario.Role);

                    // 📊 Actualizar timestamp de última conexión
                    await _userRepository.ActualizarUltimaConexionAsync(usuarioUid);

                    return RegistrarTokenFcmResult.Exito(usuario, tokenFcm);
                }
                else
                {
                    _logger.LogError("Error al actualizar token FCM en base de datos para usuario {UsuarioUid}", usuarioUid);
                    return RegistrarTokenFcmResult.Fallo(new[] { "Error interno al actualizar token FCM" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no controlada al registrar token FCM para usuario {UsuarioUid}", usuarioUid);
                return RegistrarTokenFcmResult.Fallo(new[] { "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Valida los parámetros de entrada según reglas técnicas.
        /// </summary>
        private (bool EsValido, string[] Errores) ValidarEntrada(string usuarioUid, string tokenFcm)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(usuarioUid))
                errores.Add("UID del usuario es requerido");

            if (string.IsNullOrWhiteSpace(tokenFcm))
                errores.Add("Token FCM es requerido");
            else if (tokenFcm.Length < 10) // Los tokens FCM son largos
                errores.Add("Token FCM inválido: muy corto");
            else if (tokenFcm.Length > 4096) // Límite razonable
                errores.Add("Token FCM inválido: muy largo");

            return (errores.Count == 0, errores.ToArray());
        }

        /// <summary>
        /// Valida las reglas de negocio del sistema.
        /// </summary>
        private (bool EsValido, string[] Errores) ValidarReglasSistema(Usuario usuario, string tokenFcm)
        {
            var errores = new List<string>();

            // Validar estado del usuario
            if (usuario.Estado != "activo")
                errores.Add($"Usuario debe estar activo. Estado actual: {usuario.Estado}");

            // Validar que el role permita recibir notificaciones FCM
            var rolesPermitidos = new[] { "patrullero", "operador", "victima" };
            if (!rolesPermitidos.Contains(usuario.Role.ToLower()))
                errores.Add($"Role '{usuario.Role}' no permite notificaciones FCM");

            // Validar formato del token FCM (Firebase específico)
            if (!_validadorDatos.EsTokenFcmValido(tokenFcm))
                errores.Add("Formato de token FCM inválido");

            return (errores.Count == 0, errores.ToArray());
        }
    }

    /// <summary>
    /// Result Object Pattern: Encapsula el resultado de la operación con información detallada.
    /// </summary>
    public class RegistrarTokenFcmResult
    {
        public bool Exitoso { get; private set; }
        public string[] Errores { get; private set; } = Array.Empty<string>();
        public Usuario? Usuario { get; private set; }
        public string? TokenFcm { get; private set; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

        private RegistrarTokenFcmResult() { }

        public static RegistrarTokenFcmResult Exito(Usuario usuario, string tokenFcm)
        {
            return new RegistrarTokenFcmResult
            {
                Exitoso = true,
                Usuario = usuario,
                TokenFcm = tokenFcm
            };
        }

        public static RegistrarTokenFcmResult Fallo(string[] errores)
        {
            return new RegistrarTokenFcmResult
            {
                Exitoso = false,
                Errores = errores
            };
        }
    }
}