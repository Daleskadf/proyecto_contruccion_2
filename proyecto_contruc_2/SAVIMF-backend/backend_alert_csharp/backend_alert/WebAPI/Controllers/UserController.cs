using Microsoft.AspNetCore.Mvc;
using Application.UseCases;
using WebAPI.Filters;
using WebAPI.Models;
using WebAPI.Controllers.Models;
using Domain.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly BuscarUsuarioPorDniUseCase _buscarUsuarioPorDniUseCase;
    private readonly VincularDispositivoUseCase _vincularDispositivoUseCase;
    private readonly RegistrarUsuarioUseCase _registrarUsuarioUseCase; // 🆕 Nuevo
    private readonly ListarUsuariosUseCase _listarUsuariosUseCase; // 🆕 Nuevo
    private readonly EditarUsuarioUseCase _editarUsuarioUseCase; // 🆕 Nuevo
    private readonly RegistrarTokenFcmUseCase _registrarTokenFcmUseCase; // 🆕 FCM UseCase
    private readonly IUserRepositoryFirestore _userRepository; // 🆕 Para FCM
    private readonly ILogger<UserController> _logger; // 🆕 Para logging

    public UserController(
        BuscarUsuarioPorDniUseCase buscarUsuarioPorDniUseCase,
        VincularDispositivoUseCase vincularDispositivoUseCase,
        RegistrarUsuarioUseCase registrarUsuarioUseCase, // 🆕 Nuevo
        ListarUsuariosUseCase listarUsuariosUseCase, // 🆕 Nuevo
        EditarUsuarioUseCase editarUsuarioUseCase, // 🆕 Nuevo
        RegistrarTokenFcmUseCase registrarTokenFcmUseCase, // 🆕 FCM UseCase
        IUserRepositoryFirestore userRepository, // 🆕 Para FCM
        ILogger<UserController> logger) // 🆕 Para logging
    {
        _buscarUsuarioPorDniUseCase = buscarUsuarioPorDniUseCase;
        _vincularDispositivoUseCase = vincularDispositivoUseCase;
        _registrarUsuarioUseCase = registrarUsuarioUseCase; // 🆕 Nuevo
        _listarUsuariosUseCase = listarUsuariosUseCase; // 🆕 Nuevo
        _editarUsuarioUseCase = editarUsuarioUseCase; // 🆕 Nuevo
        _registrarTokenFcmUseCase = registrarTokenFcmUseCase; // 🆕 FCM UseCase
        _userRepository = userRepository; // 🆕 Para FCM
        _logger = logger; // 🆕 Para logging
    }

    [FirebaseAuthGuardAttribute]
    [HttpGet("buscar")]
    public async Task<IActionResult> BuscarPorDni([FromQuery] string dni, [FromQuery] string? role = null)
    {
        if (string.IsNullOrWhiteSpace(dni))
            return BadRequest(new { mensaje = "Debes enviar un DNI" });

        var usuario = await _buscarUsuarioPorDniUseCase.EjecutarAsync(dni, role);
        if (usuario == null)
            return NotFound(new { mensaje = "No se encontró el usuario" });

        return Ok(usuario);
    }

    // NUEVO ENDPOINT
    [FirebaseAuthGuardAttribute]
    [HttpPost("vincular-dispositivo")]
    public async Task<IActionResult> VincularDispositivo([FromBody] VincularDispositivoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Dni) || string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest(new { mensaje = "Debes enviar DNI y deviceId" });

        try
        {
            var mensaje = await _vincularDispositivoUseCase.EjecutarAsync(dto);
            return Ok(new { mensaje });
        }
        catch (Exception ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // 🆕 ENDPOINT PARA REGISTRAR PATRULLEROS/OPERADORES
    [HttpPost("registrar")]
    public async Task<IActionResult> RegistrarUsuario([FromBody] RegistrarUsuarioRequestDto request)
    {
        Console.WriteLine($"[RegistrarUsuario] Recibida petición para registrar: {request.Email}");

        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { mensaje = "El email es requerido" });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { mensaje = "La contraseña es requerida" });

        if (string.IsNullOrWhiteSpace(request.Dni))
            return BadRequest(new { mensaje = "El DNI es requerido" });

        if (string.IsNullOrWhiteSpace(request.Nombre))
            return BadRequest(new { mensaje = "El nombre es requerido" });

        if (request.Role != "patrullero" && request.Role != "operador")
            return BadRequest(new { mensaje = "El role debe ser 'patrullero' u 'operador'" });

        try
        {
            // Ejecutar caso de uso
            string uid = await _registrarUsuarioUseCase.EjecutarAsync(
                request.Email,
                request.Password,
                request.Dni,
                request.Nombre,
                request.Role
            );

            var response = new RegistrarUsuarioResponseDto
            {
                Uid = uid,
                Email = request.Email,
                Dni = request.Dni,
                Nombre = request.Nombre,
                Role = request.Role,
                Mensaje = $"Usuario {request.Role} registrado exitosamente. Se ha enviado un email de verificación."
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[RegistrarUsuario] Error de validación: {ex.Message}");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[RegistrarUsuario] Error de argumentos: {ex.Message}");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RegistrarUsuario] Error interno: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // 🆕 Endpoint para listar usuarios
    [HttpGet("listar")]
    public async Task<IActionResult> ListarUsuarios([FromQuery] int limite = 50, [FromQuery] string? role = null)
    {
        try
        {
            var usuarios = await _listarUsuariosUseCase.EjecutarAsync(limite, role);

            return Ok(new
            {
                success = true,
                data = usuarios,
                total = usuarios.Count,
                filtroRole = role,
                limite = limite
            });
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[ListarUsuarios] Error de argumentos: {ex.Message}");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListarUsuarios] Error interno: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // 🆕 Endpoint para editar usuario
    [HttpPut("editar")]
    public async Task<IActionResult> EditarUsuario([FromBody] EditarUsuarioRequestDto request)
    {
        try
        {
            var resultado = await _editarUsuarioUseCase.EjecutarAsync(
                request.Uid,
                request.Email,
                request.Dni,
                request.Nombre,
                request.Role,
                request.Estado,
                request.EmailVerified
            );

            if (resultado)
            {
                return Ok(new
                {
                    success = true,
                    mensaje = "Usuario actualizado exitosamente",
                    uid = request.Uid
                });
            }
            else
            {
                return BadRequest(new { mensaje = "No se pudo actualizar el usuario" });
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[EditarUsuario] Error de argumentos: {ex.Message}");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EditarUsuario] Error interno: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    // 🆕 ===== ENDPOINTS FCM CON CLEAN ARCHITECTURE =====

    /// <summary>
    /// Registra o actualiza el token FCM del usuario autenticado.
    /// Implementa Clean Architecture con UseCase dedicado.
    /// POST /api/User/fcm-token
    /// </summary>
    [FirebaseAuthGuardAttribute]
    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegistrarTokenFcm([FromBody] RegistrarTokenFcmRequest request)
    {
        _logger.LogInformation("Inicio de registro de token FCM");

        try
        {
            // Validar DTO de entrada
            var validacionDto = request?.ValidarDTO();
            if (validacionDto?.EsValido == false)
            {
                _logger.LogWarning("Validación DTO falló: {Errores}", string.Join(", ", validacionDto.Value.Errores));
                return BadRequest(RegistrarTokenFcmResponse.Fallo(validacionDto.Value.Errores));
            }

            // Obtener UID del usuario autenticado
            var uid = HttpContext.Items["FirebaseUid"]?.ToString();
            if (string.IsNullOrEmpty(uid))
            {
                _logger.LogWarning("Intento de registro FCM sin autenticación");
                return Unauthorized(RegistrarTokenFcmResponse.Fallo(new[] { "Usuario no autenticado" }));
            }

            // Log para debugging
            _logger.LogDebug("Registro FCM - Usuario: {Uid}, DebugInfo: {@DebugInfo}",
                uid, request?.GetDebugInfo());

            // Ejecutar caso de uso
            var resultado = await _registrarTokenFcmUseCase.EjecutarAsync(uid, request!.TokenFcmFinal);

            if (resultado.Exitoso)
            {
                _logger.LogInformation("Token FCM registrado exitosamente para usuario {Uid} con role {Role}",
                    uid, resultado.Usuario?.Role);

                return Ok(RegistrarTokenFcmResponse.Exito(
                    resultado.Usuario!.Uid,
                    resultado.Usuario.Nombre,
                    resultado.Usuario.Role,
                    resultado.Usuario.Email
                ));
            }
            else
            {
                _logger.LogWarning("Fallo en registro FCM para usuario {Uid}: {Errores}",
                    uid, string.Join(", ", resultado.Errores));
                return BadRequest(RegistrarTokenFcmResponse.Fallo(resultado.Errores));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada en registro de token FCM");
            return StatusCode(500, RegistrarTokenFcmResponse.Fallo(new[] { "Error interno del servidor" }));
        }
    }

    /// <summary>
    /// Endpoint de heartbeat para mantener la conexión activa.
    /// Actualiza el timestamp de última conexión del usuario.
    /// POST /api/User/heartbeat
    /// </summary>
    [FirebaseAuthGuardAttribute]
    [HttpPost("heartbeat")]
    public async Task<IActionResult> ActualizarHeartbeat()
    {
        try
        {
            var uid = HttpContext.Items["FirebaseUid"]?.ToString();
            if (string.IsNullOrEmpty(uid))
            {
                return Unauthorized(new { mensaje = "Usuario no autenticado" });
            }

            await _userRepository.ActualizarUltimaConexionAsync(uid);

            _logger.LogDebug("Heartbeat actualizado para usuario {Uid}", uid);

            return Ok(new
            {
                mensaje = "Heartbeat actualizado",
                timestamp = DateTime.UtcNow,
                uid = uid
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en heartbeat");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Endpoint de diagnóstico para ver el estado de tokens FCM por role.
    /// GET /api/User/fcm-tokens/diagnostico
    /// </summary>
    [HttpGet("fcm-tokens/diagnostico")]
    public async Task<IActionResult> DiagnosticoTokensFcm([FromQuery] string? role = null)
    {
        try
        {
            var todosUsuarios = await _userRepository.ListarUsuariosAsync(limite: 500);

            // Filtrar por role si se especifica
            var usuariosFiltrados = string.IsNullOrEmpty(role)
                ? todosUsuarios
                : todosUsuarios.Where(u => u.Role?.ToLower() == role?.ToLower()).ToList();

            // Estadísticas
            var usuariosConToken = usuariosFiltrados.Where(u => !string.IsNullOrEmpty(u.FcmToken)).ToList();
            var usuariosSinToken = usuariosFiltrados.Where(u => string.IsNullOrEmpty(u.FcmToken)).ToList();
            var usuariosActivos = usuariosFiltrados.Where(u => u.Estado == "activo").ToList();

            // Agrupación por roles
            var estadisticasPorRole = todosUsuarios
                .GroupBy(u => u.Role?.ToLower() ?? "sin_role")
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        total = g.Count(),
                        conTokenFcm = g.Count(u => !string.IsNullOrEmpty(u.FcmToken)),
                        sinTokenFcm = g.Count(u => string.IsNullOrEmpty(u.FcmToken)),
                        activos = g.Count(u => u.Estado == "activo")
                    }
                );

            return Ok(new
            {
                resumen = new
                {
                    totalUsuarios = usuariosFiltrados.Count,
                    usuariosConTokenFcm = usuariosConToken.Count,
                    usuariosSinTokenFcm = usuariosSinToken.Count,
                    usuariosActivos = usuariosActivos.Count,
                    filtroRole = role ?? "todos"
                },
                estadisticasPorRole = estadisticasPorRole,
                usuariosConToken = usuariosConToken.Select(u => new
                {
                    uid = u.Uid,
                    nombre = u.Nombre,
                    email = u.Email,
                    role = u.Role,
                    estado = u.Estado,
                    tokenPreview = u.FcmToken?.Substring(0, Math.Min(20, u.FcmToken.Length)) + "...",
                    ultimaConexion = u.UltimaConexion
                }).ToList(),
                usuariosSinToken = usuariosSinToken.Select(u => new
                {
                    uid = u.Uid,
                    nombre = u.Nombre,
                    email = u.Email,
                    role = u.Role,
                    estado = u.Estado,
                    ultimaConexion = u.UltimaConexion
                }).ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en diagnóstico de tokens FCM");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }
}