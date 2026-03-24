using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;
using FirebaseAdmin.Auth; // <-- Importante
using WebAPI.Filters;
using Application.UseCases;
using Domain.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class PatrullaController : ControllerBase
{
    private readonly ActualizarUbicacionPatrullaUseCase _actualizarUbicacionUseCase;
    private readonly ListarUbicacionesPatrullasUseCase _listarUbicacionesUseCase;
    private readonly IAlertaRepository _alertaRepository;

    public PatrullaController(
        ActualizarUbicacionPatrullaUseCase actualizarUbicacionUseCase,
        ListarUbicacionesPatrullasUseCase listarUbicacionesUseCase,
        IAlertaRepository alertaRepository)
    {
        _actualizarUbicacionUseCase = actualizarUbicacionUseCase;
        _listarUbicacionesUseCase = listarUbicacionesUseCase;
        _alertaRepository = alertaRepository;
    }

    [FirebaseAuthGuardAttribute]
    [HttpPost("ubicacion")]
    public async Task<IActionResult> ActualizarUbicacionPatrulla([FromBody] UbicacionPatrullaDto body)
    {
        // Obtén el usuario Firebase guardado por el filtro (como UserRecord)
        var firebaseUser = HttpContext.Items["FirebaseUser"] as UserRecord;
        if (firebaseUser == null)
        {
            return Unauthorized(new { mensaje = "Usuario no autenticado" });
        }
        string patrulleroId = firebaseUser.Uid;

        await _actualizarUbicacionUseCase.EjecutarAsync(patrulleroId, body.lat, body.lon);

        // 🔥 DETECCIÓN AUTOMÁTICA DE LLEGADA POR GPS
        await VerificarLlegadaAutomatica(patrulleroId, body.lat, body.lon);

        return Ok(new
        {
            mensaje = "Ubicación actualizada correctamente",
            patrulleroId = patrulleroId,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 🎯 Método para detectar automáticamente si el patrullero llegó al lugar de la alerta
    /// Se ejecuta cada vez que el patrullero actualiza su ubicación
    /// </summary>
    private async Task VerificarLlegadaAutomatica(string patrulleroId, double latPatrullero, double lonPatrullero)
    {
        try
        {
            // Buscar alertas asignadas a este patrullero con estado "tomada"
            var alertas = await _alertaRepository.ListarAlertasAsync();
            var alertasTomadas = alertas.Where(a => 
                a.PatrulleroAsignado == patrulleroId && 
                a.Estado == "tomada"
            ).ToList();

            foreach (var alerta in alertasTomadas)
            {
                // Calcular distancia entre patrullero y víctima
                double distancia = CalcularDistanciaHaversine(
                    latPatrullero, lonPatrullero,
                    alerta.Lat, alerta.Lon
                );

                Console.WriteLine($"📍 Distancia patrullero {patrulleroId} a alerta {alerta.Id}: {distancia:F2}m");

                // Si está a 30 metros o menos, marcar como "llegada" automáticamente
                if (distancia <= 30)
                {
                    var updates = new Dictionary<string, object>
                    {
                        { "estado", "llegada" },
                        { "fechaLlegada", DateTime.UtcNow }
                    };

                    await _alertaRepository.UpdateFieldsAsync(alerta.Id, updates);
                    Console.WriteLine($"✅ LLEGADA AUTOMÁTICA detectada para alerta {alerta.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en verificación de llegada automática: {ex.Message}");
        }
    }

    /// <summary>
    /// 🧮 Fórmula de Haversine para calcular distancia entre dos coordenadas GPS
    /// Retorna la distancia en metros
    /// </summary>
    private double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Radio de la Tierra en metros
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c; // Distancia en metros
    }

    [FirebaseAuthGuardAttribute]
    [HttpGet("ubicaciones")]
    public async Task<IActionResult> ObtenerUbicacionesPatrullas()
    {
        try
        {
            var patrullas = await _listarUbicacionesUseCase.EjecutarAsync();

            var result = patrullas.Select(p => new PatrullaUbicacionDto(
                p.PatrulleroId,
                p.Lat,
                p.Lon,
                p.Timestamp
            )).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo ubicaciones de patrullas: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// � Endpoint para obtener la ubicación de una patrulla específica
    /// </summary>
    [FirebaseAuthGuardAttribute]
    [HttpGet("ubicaciones/{patrulleroId}")]
    public async Task<IActionResult> ObtenerUbicacionPatrulla(string patrulleroId)
    {
        try
        {
            var patrullas = await _listarUbicacionesUseCase.EjecutarAsync();
            var patrulla = patrullas.FirstOrDefault(p => p.PatrulleroId == patrulleroId);

            if (patrulla == null)
            {
                return NotFound(new { mensaje = $"No se encontró la patrulla con ID: {patrulleroId}" });
            }

            var result = new PatrullaUbicacionDto(
                patrulla.PatrulleroId,
                patrulla.Lat,
                patrulla.Lon,
                patrulla.Timestamp
            );

            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo ubicación de patrulla {patrulleroId}: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// �📊 Endpoint para obtener estadísticas de patrullas
    /// </summary>
    [FirebaseAuthGuardAttribute]
    [HttpGet("estadisticas")]
    public async Task<IActionResult> ObtenerEstadisticasPatrullas()
    {
        try
        {
            var patrullas = await _listarUbicacionesUseCase.EjecutarAsync();

            var ahora = DateTime.UtcNow;
            var patrullasActivas = patrullas.Where(p =>
                (ahora - p.Timestamp).TotalMinutes <= 10 // Activa si reportó en los últimos 10 min
            ).Count();

            return Ok(new
            {
                totalPatrullas = patrullas.Count,
                patrullasActivas = patrullasActivas,
                patrullasInactivas = patrullas.Count - patrullasActivas,
                ultimaActualizacion = ahora,
                patrullas = patrullas.Select(p => new
                {
                    patrulleroId = p.PatrulleroId,
                    estado = (ahora - p.Timestamp).TotalMinutes <= 10 ? "Activa" : "Inactiva",
                    minutosDesdeUltimaActualizacion = Math.Round((ahora - p.Timestamp).TotalMinutes, 1)
                })
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error interno del servidor" });
        }
    }
}