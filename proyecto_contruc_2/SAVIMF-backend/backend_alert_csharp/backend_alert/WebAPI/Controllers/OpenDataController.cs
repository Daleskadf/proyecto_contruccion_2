using Microsoft.AspNetCore.Mvc;
using backend_alert.Application.UseCases;
using backend_alert.Domain.Interfaces;
using System.Text;

namespace backend_alert.WebAPI.Controllers;


[ApiController]
[Route("api/[controller]")]
public class OpenDataController : ControllerBase
{
    private readonly ObtenerOpenDataUseCase _useCase;
    private readonly IOpenDataRepository _repository;
    private readonly ILogger<OpenDataController> _logger;

    public OpenDataController(
        ObtenerOpenDataUseCase useCase,
        IOpenDataRepository repository,
        ILogger<OpenDataController> logger)
    {
        _useCase = useCase;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene incidentes individuales por periodo
    /// GET /api/opendata/incidentes?anio=2025&mes=11
    /// </summary>
    [HttpGet("incidentes")]
    public async Task<IActionResult> ObtenerIncidentes([FromQuery] int anio, [FromQuery] int? mes = null)
    {
        try
        {
            var incidentes = await _useCase.ObtenerIncidentesPorPeriodoAsync(anio, mes);
            
            return Ok(new 
            { 
                anio,
                mes = mes ?? 0,
                total = incidentes.Count,
                incidentes,
                metadata = new
                {
                    fuente = "Sistema SAVIMF - Tacna, Perú",
                    actualizacion = DateTime.UtcNow,
                    privacidad = "Datos anonimizados - Coordenadas redondeadas a 3 decimales (≈111m)",
                    notas = "No incluye datos personales de víctimas ni identificadores de dispositivos"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener incidentes de Open Data");
            return StatusCode(500, new { mensaje = "Error al obtener datos" });
        }
    }

    /// <summary>
    /// Obtiene incidentes de un distrito específico
    /// GET /api/opendata/incidentes/distrito/{distrito}
    /// </summary>
    [HttpGet("incidentes/distrito/{distrito}")]
    public async Task<IActionResult> ObtenerIncidentesPorDistrito(string distrito)
    {
        try
        {
            var incidentes = await _useCase.ObtenerIncidentesPorDistritoAsync(distrito);
            
            return Ok(new 
            { 
                distrito,
                total = incidentes.Count,
                incidentes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener incidentes del distrito {Distrito}", distrito);
            return StatusCode(500, new { mensaje = "Error al obtener datos" });
        }
    }

    /// <summary>
    /// Obtiene estadísticas agregadas de un distrito/periodo
    /// GET /api/opendata/estadisticas?distrito=Centro&anio=2025&mes=11
    /// </summary>
    [HttpGet("estadisticas")]
    public async Task<IActionResult> ObtenerEstadisticas(
        [FromQuery] string distrito, 
        [FromQuery] int anio, 
        [FromQuery] int mes)
    {
        try
        {
            var agregado = await _useCase.ObtenerEstadisticasAgregadasAsync(distrito, anio, mes);
            
            if (agregado == null)
            {
                return NotFound(new 
                { 
                    mensaje = $"No hay datos para {distrito} en {anio}-{mes:D2}" 
                });
            }

            // Incluir métricas calculadas
            var metricas = agregado.ObtenerMetricas();

            return Ok(new 
            { 
                agregado,
                metricas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas agregadas");
            return StatusCode(500, new { mensaje = "Error al obtener estadísticas" });
        }
    }

    /// <summary>
    /// Obtiene todas las estadísticas de un año
    /// GET /api/opendata/estadisticas/anio/{anio}
    /// </summary>
    [HttpGet("estadisticas/anio/{anio}")]
    public async Task<IActionResult> ObtenerEstadisticasAnuales(int anio)
    {
        try
        {
            var agregados = await _useCase.ObtenerEstadisticasAnualesAsync(anio);
            
            return Ok(new 
            { 
                anio,
                totalDistritos = agregados.Select(a => a.Distrito).Distinct().Count(),
                totalIncidentes = agregados.Sum(a => a.TotalIncidentes),
                agregados
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas anuales");
            return StatusCode(500, new { mensaje = "Error al obtener estadísticas" });
        }
    }

    /// <summary>
    /// Dashboard global con métricas generales
    /// GET /api/opendata/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboardGlobal()
    {
        try
        {
            var estadisticas = await _useCase.ObtenerEstadisticasGlobalesAsync();
            return Ok(estadisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard global");
            return StatusCode(500, new { mensaje = "Error al obtener dashboard" });
        }
    }

    /// <summary>
    /// Descarga incidentes en formato CSV con TODOS los campos expandidos
    /// GET /api/opendata/descargar/csv?anio=2025&mes=11
    /// </summary>
    [HttpGet("descargar/csv")]
    public async Task<IActionResult> DescargarCSV([FromQuery] int anio, [FromQuery] int? mes = null)
    {
        try
        {
            // Asegurar que la colección `open_data` esté regenerada para el periodo solicitado
            // Si se pasó mes -> regenerar solo ese mes; si no -> regenerar todos los meses del año
            try
            {
                if (mes.HasValue)
                {
                    await _repository.RegenerarAgregadosAsync(anio, mes.Value);
                }
                else
                {
                    for (int m = 1; m <= 12; m++)
                    {
                        await _repository.RegenerarAgregadosAsync(anio, m);
                    }
                }
            }
            catch (Exception exReg)
            {
                // No bloquear la descarga si la regeneración falla, pero loggear
                _logger.LogWarning(exReg, "Error regenerando open_data antes de generar CSV para {Anio}-{Mes}", anio, mes ?? 0);
            }

            var incidentes = await _useCase.ObtenerIncidentesPorPeriodoAsync(anio, mes);
            
            // Generar CSV con TODOS los campos nuevos
            var csv = new StringBuilder();
            
            // 🔥 HEADER COMPLETO con todos los campos
            csv.AppendLine(
                "Id," +
                "FechaCreacionAlerta,FechaTomadaAlerta,FechaLlegada,FechaAtendida,FechaResuelto," +
                "TiempoRespuestaMinutos,TiempoLlegadaMinutos,TiempoAtencionMinutos,TiempoTotalMinutos," +
                "FechaIncidente,Anio,Mes,Dia,DiaSemana,Hora,Minuto," +
                "CantidadActivaciones,EsRecurrente,EstadoFinal,NivelUrgencia," +
                "Distrito,LatitudRedondeada,LongitudRedondeada," +
                "TipoViolencia,NivelRiesgo,AlertaVeridica," +
                "EdadVictimaRango,GeneroVictima," +
                "RequirioAmbulancia,RequirioRefuerzo,VictimaTrasladadaComisaria," +
                "BateriaNivel,DispositivoTipo," +
                "FechaRegistroOpenData"
            );
            
            foreach (var inc in incidentes)
            {
                csv.AppendLine(
                    $"{inc.Id}," +
                    $"{inc.FechaCreacionAlerta?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{inc.FechaTomadaAlerta?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{inc.FechaLlegada?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{inc.FechaAtendida?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{inc.FechaResuelto?.ToString("yyyy-MM-dd HH:mm:ss")}," +
                    $"{inc.TiempoRespuestaMinutos?.ToString("F2")}," +
                    $"{inc.TiempoLlegadaMinutos?.ToString("F2")}," +
                    $"{inc.TiempoAtencionMinutos?.ToString("F2")}," +
                    $"{inc.TiempoTotalMinutos?.ToString("F2")}," +
                    $"{inc.FechaIncidente:yyyy-MM-dd HH:mm:ss}," +
                    $"{inc.Anio},{inc.Mes},{inc.Dia}," +
                    $"{inc.DiaSemanaNombre},{inc.HoraDelDia},{inc.MinutoDelDia}," +
                    $"{inc.CantidadActivaciones},{inc.EsRecurrente},{inc.EstadoFinal},{inc.NivelUrgencia}," +
                    $"{inc.Distrito},{inc.LatitudRedondeada},{inc.LongitudRedondeada}," +
                    $"{inc.TipoViolencia},{inc.NivelRiesgo},{inc.AlertaVeridica}," +
                    $"{inc.EdadVictimaRango},{inc.GeneroVictima}," +
                    $"{inc.RequirioAmbulancia},{inc.RequirioRefuerzo},{inc.VictimaTrasladadaComisaria}," +
                    $"{inc.BateriaNivel},{inc.DispositivoTipo}," +
                    $"{inc.FechaRegistroOpenData:yyyy-MM-dd HH:mm:ss}"
                );
            }

            var fileName = $"open_data_tacna_completo_{anio}_{mes:D2}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            
            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar CSV");
            return StatusCode(500, new { mensaje = "Error al generar archivo CSV" });
        }
    }

    /// <summary>
    /// Descarga datos agregados en formato JSON
    /// GET /api/opendata/descargar/json?anio=2025
    /// </summary>
    [HttpGet("descargar/json")]
    public async Task<IActionResult> DescargarJSON([FromQuery] int anio)
    {
        try
        {
            // Regenerar open_data para todo el año antes de calcular agregados
            try
            {
                for (int m = 1; m <= 12; m++)
                    await _repository.RegenerarAgregadosAsync(anio, m);
            }
            catch (Exception exReg)
            {
                _logger.LogWarning(exReg, "Error regenerando open_data antes de generar JSON anual para {Anio}", anio);
            }

            var agregados = await _useCase.ObtenerEstadisticasAnualesAsync(anio);
            
            var data = new
            {
                metadata = new
                {
                    anio,
                    fechaGeneracion = DateTime.UtcNow,
                    fuente = "Sistema SAVIMF - Tacna, Perú",
                    totalDistritos = agregados.Select(a => a.Distrito).Distinct().Count(),
                    totalIncidentes = agregados.Sum(a => a.TotalIncidentes)
                },
                estadisticas = agregados.Select(a => new
                {
                    a.Distrito,
                    periodo = $"{a.Anio}-{a.Mes:D2}",
                    a.TotalIncidentes,
                    a.TasaVeracidad,
                    tiposViolencia = new
                    {
                        a.ViolenciaFisica,
                        a.ViolenciaPsicologica,
                        a.ViolenciaSexual,
                        a.ViolenciaEconomica
                    },
                    nivelesRiesgo = new
                    {
                        a.RiesgoBajo,
                        a.RiesgoMedio,
                        a.RiesgoAlto,
                        a.RiesgoCritico
                    },
                    distribucionEdades = new
                    {
                        menores = a.VictimasMenores,
                        jovenes18_29 = a.Victimas18_29,
                        adultos30_44 = a.Victimas30_44,
                        adultos45_59 = a.Victimas45_59,
                        mayores60 = a.Victimas60Plus
                    },
                    recursosMovilizados = new
                    {
                        a.CasosConAmbulancia,
                        a.CasosConRefuerzo
                    },
                    ubicacionCentral = new
                    {
                        latitud = a.LatitudCentral,
                        longitud = a.LongitudCentral
                    }
                })
            };

            var fileName = $"open_data_tacna_agregado_{anio}.json";
            return File(
                Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                })),
                "application/json",
                fileName
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar JSON");
            return StatusCode(500, new { mensaje = "Error al generar archivo JSON" });
        }
    }

    // Nota: La regeneración on-demand ahora se realiza automáticamente antes de las descargas CSV/JSON
}
