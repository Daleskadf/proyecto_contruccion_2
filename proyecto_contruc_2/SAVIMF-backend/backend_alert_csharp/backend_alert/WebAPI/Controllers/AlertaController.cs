using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Domain.Entities;
using WebAPI.Filters;
using Application.UseCases;
using WebAPI.Models;
using Domain.Interfaces;
using WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;


[ApiController]
[Route("api/[controller]")]
public class AlertaController : ControllerBase
{
    private readonly RegistrarAlertaUseCase _registrarAlertaUseCase;
    private readonly IUserRepositoryFirestore _userRepository; 
    private readonly ListarAlertasUseCase _listarAlertasUseCase;
    private readonly IHubContext<AlertaHub> _hubContext;
    private readonly IAlertaRepository _alertaRepository;
    private readonly IFCMService _fcmService; 

    public AlertaController(
    RegistrarAlertaUseCase registrarAlertaUseCase,
    ListarAlertasUseCase listarAlertasUseCase,
    IUserRepositoryFirestore userRepository,
    IHubContext<AlertaHub> hubContext,
    IAlertaRepository alertaRepository,
    IFCMService fcmService) 
    {
        _registrarAlertaUseCase = registrarAlertaUseCase;
        _listarAlertasUseCase = listarAlertasUseCase;
        _userRepository = userRepository;
        _hubContext = hubContext;
        _alertaRepository = alertaRepository;
        _fcmService = fcmService; 
    }

    [HttpPost("lorawan-webhook")]
    public async Task<IActionResult> RegistrarLorawanWebhook([FromBody] JsonElement data)
    {
        try
        {
            Console.WriteLine(data.ToString()); // Log para depuración

            // Extraer datos del webhook
            var datosExtraidos = ExtraerDatosLorawanWebhook(data);

            if (string.IsNullOrEmpty(datosExtraidos.DevEUI) || 
                datosExtraidos.Lat == null || 
                datosExtraidos.Lon == null || 
                datosExtraidos.Bateria == null)
            {
                return BadRequest(new { mensaje = "Datos incompletos o inválidos" });
            }

            // Buscar víctima por deviceId
            var datosVictima = await ObtenerDatosVictima(datosExtraidos.DeviceId);

            // Crear alerta
            var alerta = new Alerta(
                datosExtraidos.DevEUI,
                datosExtraidos.Lat.Value,
                datosExtraidos.Lon.Value,
                datosExtraidos.Bateria.Value,
                datosExtraidos.Timestamp,
                datosExtraidos.DeviceId ?? string.Empty,
                $"{datosVictima.Nombre} {datosVictima.Apellido}"
            );

            await _registrarAlertaUseCase.EjecutarAsync(alerta);

            // Enviar notificaciones
            await EnviarNotificacionesAlerta(datosExtraidos, datosVictima);

            // Respuesta HTTP
            return Ok(new
            {
                estado = "Despachada",
                nombre = datosVictima.Nombre,
                apellido = datosVictima.Apellido,
                dni = datosVictima.Dni,
                lat = datosExtraidos.Lat,
                lon = datosExtraidos.Lon,
                bateria = datosExtraidos.Bateria,
                timestamp = datosExtraidos.Timestamp,
                device_id = datosExtraidos.DeviceId
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando webhook LoRaWAN: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno al procesar webhook" });
        }
    }

    /// <summary>
    /// Extrae datos del webhook LoRaWAN (DevEUI, GPS, batería, timestamp)
    /// </summary>
    private DatosLorawanDto ExtraerDatosLorawanWebhook(JsonElement data)
    {
        var datos = new DatosLorawanDto
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // DevEUI y DeviceId
            if (data.TryGetProperty("end_device_ids", out JsonElement endDeviceIds))
            {
                if (endDeviceIds.TryGetProperty("dev_eui", out JsonElement devEuiProp))
                    datos.DevEUI = devEuiProp.GetString();

                if (endDeviceIds.TryGetProperty("device_id", out JsonElement deviceIdProp))
                    datos.DeviceId = deviceIdProp.GetString();
            }

            // Uplink_message y payload
            if (data.TryGetProperty("uplink_message", out JsonElement uplinkMessage) &&
                uplinkMessage.TryGetProperty("frm_payload", out JsonElement frmPayloadProp) &&
                frmPayloadProp.ValueKind == JsonValueKind.String)
            {
                var frmPayloadBase64 = frmPayloadProp.GetString();
                if (!string.IsNullOrEmpty(frmPayloadBase64))
                {
                    try
                    {
                        byte[] decodedBytes = Convert.FromBase64String(frmPayloadBase64);
                        string payloadDecoded = System.Text.Encoding.UTF8.GetString(decodedBytes);

                        using var doc = JsonDocument.Parse(payloadDecoded);
                        var payloadJson = doc.RootElement;

                        // GPS
                        if (payloadJson.TryGetProperty("GPS", out JsonElement gpsProp))
                        {
                            var gpsString = gpsProp.GetString();
                            if (!string.IsNullOrEmpty(gpsString))
                            {
                                var coords = gpsString.Split(',');
                                if (coords.Length == 2 &&
                                    double.TryParse(coords[0], out var latVal) &&
                                    double.TryParse(coords[1], out var lonVal))
                                {
                                    datos.Lat = latVal;
                                    datos.Lon = lonVal;
                                }
                            }
                        }

                        // Batería
                        if (payloadJson.TryGetProperty("Battery", out JsonElement batteryProp) &&
                            batteryProp.TryGetDouble(out var batteryVal))
                        {
                            datos.Bateria = batteryVal;
                        }
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Formato de frm_payload inválido (no es Base64 válido)");
                    }
                }
            }

            // Fallback: ubicación por locations.user
            if ((datos.Lat == null || datos.Lon == null) &&
                data.TryGetProperty("uplink_message", out JsonElement uplinkMessage2) &&
                uplinkMessage2.TryGetProperty("locations", out JsonElement locations) &&
                locations.TryGetProperty("user", out JsonElement user))
            {
                if (user.TryGetProperty("latitude", out JsonElement latProp) &&
                    user.TryGetProperty("longitude", out JsonElement lonProp) &&
                    latProp.TryGetDouble(out var latVal) &&
                    lonProp.TryGetDouble(out var lonVal))
                {
                    datos.Lat = latVal;
                    datos.Lon = lonVal;
                }
            }

            // Timestamp
            if (data.TryGetProperty("received_at", out JsonElement receivedAtProp) &&
                receivedAtProp.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(receivedAtProp.GetString(), out var ts))
            {
                datos.Timestamp = ts;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error parseando datos LoRaWAN: {ex.Message}");
        }

        return datos;
    }

    /// <summary>
    /// Obtiene datos de la víctima desde el repositorio de usuarios
    /// </summary>
    private async Task<DatosVictima> ObtenerDatosVictima(string? deviceId)
    {
        var datosVictima = new DatosVictima
        {
            Nombre = "Sin asignar",
            Apellido = "",
            Dni = ""
        };

        if (string.IsNullOrEmpty(deviceId))
            return datosVictima;

        try
        {
            var victima = await _userRepository.BuscarPorDeviceIdAsync(deviceId);
            if (victima != null)
            {
                datosVictima.Nombre = victima.Nombre;
                datosVictima.Apellido = victima.Apellido;
                datosVictima.Dni = victima.Dni;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obtener datos de víctima: {ex.Message}");
        }

        return datosVictima;
    }

    /// <summary>
    /// Envía notificaciones SignalR y FCM
    /// </summary>
    private async Task EnviarNotificacionesAlerta(DatosLorawanDto datosAlerta, DatosVictima datosVictima)
    {
        // Enviar por SignalR
        try
        {
            await _hubContext.Clients.All.SendAsync("RecibirAlerta", new
            {
                estado = "Despachada",
                nombre = datosVictima.Nombre,
                apellido = datosVictima.Apellido,
                dni = datosVictima.Dni,
                lat = datosAlerta.Lat,
                lon = datosAlerta.Lon,
                bateria = datosAlerta.Bateria,
                timestamp = datosAlerta.Timestamp,
                device_id = datosAlerta.DeviceId,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando notificación SignalR: {ex.Message}");
        }

        // Enviar por FCM
        try
        {
            var tokensFcm = await _userRepository.ObtenerTokensFcmPorRoleAsync("patrullero", soloActivos: true);

            if (tokensFcm.Any())
            {
                var alertaData = new Dictionary<string, string>
                {
                    ["type"] = "emergency_alert",
                    ["lat"] = datosAlerta.Lat?.ToString() ?? "0",
                    ["lon"] = datosAlerta.Lon?.ToString() ?? "0",
                    ["nombre"] = datosVictima.Nombre,
                    ["apellido"] = datosVictima.Apellido,
                    ["dni"] = datosVictima.Dni,
                    ["device_id"] = datosAlerta.DeviceId ?? "",
                    ["timestamp"] = datosAlerta.Timestamp.ToString("O"),
                    ["bateria"] = datosAlerta.Bateria?.ToString() ?? "0"
                };

                string tituloNotificacion = "Nueva Alerta de Emergencia";
                string cuerpoNotificacion = !string.IsNullOrEmpty(datosVictima.Nombre) && datosVictima.Nombre != "Sin asignar"
                    ? $"Alerta de {datosVictima.Nombre} {datosVictima.Apellido}"
                    : "Nueva alerta de emergencia detectada";

                int notificacionesEnviadas = await _fcmService.EnviarNotificacionMultipleAsync(
                    tokensFcm,
                    tituloNotificacion,
                    cuerpoNotificacion,
                    alertaData
                );

                Console.WriteLine($"Notificaciones FCM enviadas: {notificacionesEnviadas}/{tokensFcm.Count}");
            }
            else
            {
                Console.WriteLine("No se encontraron tokens FCM activos para patrulleros");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando notificaciones FCM: {ex.Message}");
            // No interrumpir el flujo si FCM falla
        }
    }

    /// <summary>
    /// DTO para datos extraídos del webhook LoRaWAN
    /// </summary>
    private class DatosLorawanDto
    {
        public string? DevEUI { get; set; }
        public string? DeviceId { get; set; }
        public double? Lat { get; set; }
        public double? Lon { get; set; }
        public double? Bateria { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// DTO para datos de la víctima
    /// </summary>
    private class DatosVictima
    {
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Dni { get; set; } = "";
    }

    [FirebaseAuthGuardAttribute]
    [HttpGet("listar")]
    public async Task<IActionResult> ListarAlertas()
    {
        try
        {
            var alertas = await _listarAlertasUseCase.EjecutarAsync();
            var result = alertas.Select(a => new
            {
                id = a.Id,  //¡IMPORTANTE! Incluir el ID del documento
                estado = a.Estado ?? "disponible",  // Usar el estado real de la BD
                nombre = string.IsNullOrWhiteSpace(a.NombreVictima) ? "Sin asignar" : a.NombreVictima,
                lat = a.Lat,
                lon = a.Lon,
                bateria = a.Bateria,
                timestamp = a.Timestamp,
                device_id = a.DeviceId,
                // NUEVOS CAMPOS SISTEMA DE PRIORIDADES
                cantidadActivaciones = a.CantidadActivaciones,
                ultimaActivacion = a.UltimaActivacion,
                nivelUrgencia = a.NivelUrgencia,
                esRecurrente = a.EsRecurrente,
                devEUI = a.DevEUI,
                patrulleroAsignado = a.PatrulleroAsignado ?? "",
                fechaTomada = a.FechaTomada,
                fechaLlegada = a.FechaLlegada,
                fechaAtendida = a.FechaAtendida,
                fechaResuelto = a.FechaResuelto
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al listar alertas: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno al listar alertas" });
        }
    }

    // ==================================================
    // Endpoint: Obtener alertas por rango de fechas
    // GET /api/alerta/rango?fechaInicio=2025-11-01&fechaFin=2025-11-30
    // Reusa el repositorio de alertas para consultas por rango (usado por frontend en mapas/analisis)
    // ==================================================
    [FirebaseAuthGuardAttribute]
    [HttpGet("rango")]
    public async Task<IActionResult> ObtenerAlertasPorRango([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
    {
        try
        {
            // Validar parámetros
            if (!fechaInicio.HasValue || !fechaFin.HasValue)
                return BadRequest(new { mensaje = "fechaInicio y fechaFin son requeridos" });

            if (fechaInicio > fechaFin)
                return BadRequest(new { mensaje = "fechaInicio debe ser menor a fechaFin" });

            var alertas = await _alertaRepository.ObtenerAlertasPorRangoFechas(fechaInicio.Value, fechaFin.Value);

            var result = alertas.Select(a => new
            {
                id = a.Id,
                estado = a.Estado ?? "disponible",
                nombre = string.IsNullOrWhiteSpace(a.NombreVictima) ? "Sin asignar" : a.NombreVictima,
                lat = a.Lat,
                lon = a.Lon,
                bateria = a.Bateria,
                timestamp = a.FechaCreacion,
                device_id = a.DeviceId,
                fechaCreacion = a.FechaCreacion,
                patrulleroAsignado = a.PatrulleroAsignado ?? "",
                cantidadActivaciones = a.CantidadActivaciones,
                ultimaActivacion = a.UltimaActivacion,
                nivelUrgencia = a.NivelUrgencia,
                esRecurrente = a.EsRecurrente,
                devEUI = a.DevEUI,
                fechaTomada = a.FechaTomada,
                fechaLlegada = a.FechaLlegada,
                fechaAtendida = a.FechaAtendida,
                fechaResuelto = a.FechaResuelto
            }).ToList();

            return Ok(new { fechaInicio, fechaFin, total = result.Count, alertas = result });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo alertas por rango: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error al obtener alertas por rango" });
        }
    }

    // ==================================================
    // Endpoint: Tomar una alerta (asignar patrullero)
    // POST /api/alerta/tomar
    // Body: { "alertaId": "id", "patrulleroId": "patrullero_123" }
    // ==================================================
    [FirebaseAuthGuardAttribute]
    [HttpPost("tomar")]
    public async Task<IActionResult> TomarAlerta([FromBody] WebAPI.Models.TomarAlertaRequestDto body)
    {
        if (body == null || string.IsNullOrEmpty(body.alertaId) || string.IsNullOrEmpty(body.patrulleroId))
            return BadRequest(new { mensaje = "alertaId y patrulleroId son requeridos" });

        try
        {
            var updates = new Dictionary<string, object>
            {
                { "patrulleroAsignado", body.patrulleroId },
                { "estado", "tomada" },
                { "fechaTomada", DateTime.UtcNow }
            };

            await _alertaRepository.UpdateFieldsAsync(body.alertaId, updates);
            return Ok(new { mensaje = "Alerta tomada correctamente" });
        }
        catch (Exception ex) when (ex.Message.Contains("No document to update"))
        {
            Console.WriteLine($"Alerta no encontrada: {body?.alertaId}");
            return NotFound(new
            {
                mensaje = $"La alerta con ID '{body?.alertaId ?? "N/A"}' no existe",
                alertaIdEnviado = body?.alertaId ?? "N/A",
                sugerencia = "Verifica que el ID de la alerta sea correcto"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error tomando alerta {body?.alertaId}: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno al tomar la alerta" });
        }
    }

    // ==================================================
    // Endpoint: Cambiar estado de una alerta
    // POST /api/alerta/cambiar-estado
    // Body: { "alertaId": "id", "patrulleroId": "patrullero_123", "nuevoEstado": "llegada" }
    // ==================================================
    [FirebaseAuthGuardAttribute]
    [HttpPost("cambiar-estado")]
    public async Task<IActionResult> CambiarEstado([FromBody] WebAPI.Models.CambiarEstadoRequestDto body)
    {
        if (body == null || string.IsNullOrEmpty(body.alertaId) || string.IsNullOrEmpty(body.nuevoEstado))
            return BadRequest(new { mensaje = "alertaId y nuevoEstado son requeridos" });

        try
        {
            var updates = new Dictionary<string, object>
            {
                { "estado", body.nuevoEstado }
            };

            if (!string.IsNullOrEmpty(body.patrulleroId))
                updates["patrulleroAsignado"] = body.patrulleroId;

            // Ajustar marcas de tiempo según el nuevo estado
            if (body.nuevoEstado.Equals("llegada", StringComparison.OrdinalIgnoreCase))
            {
                updates["fechaLlegada"] = DateTime.UtcNow;
            }
            else if (body.nuevoEstado.Equals("atendida", StringComparison.OrdinalIgnoreCase))
            {
                updates["fechaAtendida"] = DateTime.UtcNow;
            }
            else if (body.nuevoEstado.Equals("resuelto", StringComparison.OrdinalIgnoreCase)
                     || body.nuevoEstado.Equals("resuelta", StringComparison.OrdinalIgnoreCase)
                     || body.nuevoEstado.Equals("finalizada", StringComparison.OrdinalIgnoreCase))
            {
                updates["fechaResuelto"] = DateTime.UtcNow;
            }

            await _alertaRepository.UpdateFieldsAsync(body.alertaId, updates);
            return Ok(new { mensaje = "Estado de alerta actualizado" });
        }
        catch (Exception ex) when (ex.Message.Contains("No document to update"))
        {
            Console.WriteLine($"Alerta no encontrada: {body?.alertaId}");
            return NotFound(new
            {
                mensaje = $"La alerta con ID '{body?.alertaId}' no existe",
                alertaIdEnviado = body?.alertaId,
                sugerencia = "Verifica que el ID de la alerta sea correcto"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cambiando estado de alerta {body?.alertaId}: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno al cambiar el estado" });
        }
    }

    [FirebaseAuthGuardAttribute]
    [HttpGet("cantidad")]
    public async Task<IActionResult> CantidadAlertas()
    {
        try
        {
            var alertas = await _alertaRepository.ListarAlertasActivasAsync();
            int cantidad = alertas.Count;
            
            return Ok(new { 
                cantidad = cantidad,
                mensaje = "OK"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener cantidad de alertas: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error al obtener cantidad de alertas" });
        }
    }

    // 📋 ENDPOINT PARA OBTENER SOLO ALERTAS ACTIVAS (optimizado para frontend)
    [FirebaseAuthGuardAttribute]
    [HttpGet("activas")]
    public async Task<IActionResult> ObtenerAlertasActivas()
    {
        try
        {
            await _alertaRepository.ArchivarAlertasVencidas(); // Auto-archivar antes de listar

            var alertas = await _alertaRepository.ListarAlertasActivasAsync();
            var result = alertas.Select(a => new
            {
                id = a.Id,
                estado = a.Estado ?? "disponible",
                nombre = string.IsNullOrWhiteSpace(a.NombreVictima) ? "Sin asignar" : a.NombreVictima,
                lat = a.Lat,
                lon = a.Lon,
                bateria = a.Bateria,
                timestamp = a.Timestamp,
                device_id = a.DeviceId,
                fechaCreacion = a.FechaCreacion,
                patrulleroAsignado = a.PatrulleroAsignado ?? "",

                // NUEVOS CAMPOS PARA FRONTEND
                cantidadActivaciones = a.CantidadActivaciones,
                ultimaActivacion = a.UltimaActivacion,
                nivelUrgencia = a.NivelUrgencia,
                esRecurrente = a.EsRecurrente
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener alertas activas: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno al obtener alertas activas" });
        }
    }
}