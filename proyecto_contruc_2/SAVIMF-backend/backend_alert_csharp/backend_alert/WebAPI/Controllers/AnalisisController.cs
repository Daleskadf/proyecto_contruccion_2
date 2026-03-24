using Microsoft.AspNetCore.Mvc;
using Domain.Interfaces;
using WebAPI.Filters;

[ApiController]
[Route("api/[controller]")]
public class AnalisisController : ControllerBase
{
    private readonly IAlertaRepository _alertaRepository;

    public AnalisisController(IAlertaRepository alertaRepository)
    {
        _alertaRepository = alertaRepository;
    }
    [FirebaseAuthGuardAttribute]
    [HttpGet("mapa-calor")]
    public async Task<IActionResult> ObtenerMapaCalor(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin,
        [FromQuery] string? tipoFiltro = null) // "mes", "semana", "dia"
    {
        try
        {
            var alertas = await _alertaRepository.ObtenerAlertasPorRangoFechas(fechaInicio, fechaFin);

            var puntosCalor = alertas
                .Where(a => a.Estado != "vencida") // Excluir vencidas
                .GroupBy(a => new
                {
                    Lat = Math.Round(a.Lat, 4), // Agrupar por coordenadas aproximadas
                    Lon = Math.Round(a.Lon, 4)
                })
                .Select(g => new
                {
                    lat = g.Key.Lat,
                    lng = g.Key.Lon,
                    intensidad = g.Count(),
                    alertas = g.Select(a => new
                    {
                        id = a.Id,
                        fecha = a.FechaCreacion,
                        tipo = a.NivelUrgencia,
                        nombre = a.NombreVictima
                    }).ToList()
                })
                .OrderByDescending(p => p.intensidad)
                .ToList();

            return Ok(new
            {
                fechaInicio,
                fechaFin,
                totalAlertas = alertas.Count,
                puntosCalor = puntosCalor,
                estadisticas = new
                {
                    zonasMasActivas = puntosCalor.Take(5),
                    promedioAlertasPorDia = alertas.Count / Math.Max(1, (fechaFin - fechaInicio).Days),
                    tiposSeveridad = alertas.GroupBy(a => a.NivelUrgencia)
                        .Select(g => new { tipo = g.Key, cantidad = g.Count() })
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en mapa de calor: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error generando mapa de calor" });
        }
    }

    // 📈 ENDPOINT PARA TENDENCIAS TEMPORALES
    [FirebaseAuthGuardAttribute]
    [HttpGet("tendencias")]
    public async Task<IActionResult> ObtenerTendencias(
        [FromQuery] DateTime fechaInicio,
        [FromQuery] DateTime fechaFin,
        [FromQuery] string agrupacion = "dia") // "hora", "dia", "semana", "mes"
    {
        try
        {
            var alertas = await _alertaRepository.ObtenerAlertasPorRangoFechas(fechaInicio, fechaFin);

            object resultado;

            switch (agrupacion.ToLower())
            {
                case "hora":
                    var tendenciasHora = alertas.GroupBy(a => new { a.FechaCreacion.Date, a.FechaCreacion.Hour })
                        .Select(g => new
                        {
                            periodo = $"{g.Key.Date:yyyy-MM-dd} {g.Key.Hour:D2}:00",
                            fecha = g.Key.Date.AddHours(g.Key.Hour),
                            totalAlertas = g.Count(),
                            tipos = g.GroupBy(a => a.NivelUrgencia).Select(t => new { tipo = t.Key, cantidad = t.Count() })
                        }).OrderBy(x => x.fecha);
                    resultado = tendenciasHora;
                    break;

                case "semana":
                    var tendenciasSemana = alertas.GroupBy(a => GetWeekOfYear(a.FechaCreacion))
                        .Select(g => new
                        {
                            periodo = g.Key,
                            totalAlertas = g.Count(),
                            tipos = g.GroupBy(a => a.NivelUrgencia).Select(t => new { tipo = t.Key, cantidad = t.Count() })
                        }).OrderBy(x => x.periodo);
                    resultado = tendenciasSemana;
                    break;

                case "mes":
                    var tendenciasMes = alertas.GroupBy(a => new { a.FechaCreacion.Year, a.FechaCreacion.Month })
                        .Select(g => new
                        {
                            periodo = $"{g.Key.Year}-{g.Key.Month:D2}",
                            fecha = new DateTime(g.Key.Year, g.Key.Month, 1),
                            totalAlertas = g.Count(),
                            tipos = g.GroupBy(a => a.NivelUrgencia).Select(t => new { tipo = t.Key, cantidad = t.Count() })
                        }).OrderBy(x => x.fecha);
                    resultado = tendenciasMes;
                    break;

                default: // "dia"
                    var tendenciasDia = alertas.GroupBy(a => a.FechaCreacion.Date)
                        .Select(g => new
                        {
                            periodo = g.Key.ToString("yyyy-MM-dd"),
                            fecha = g.Key,
                            totalAlertas = g.Count(),
                            tipos = g.GroupBy(a => a.NivelUrgencia).Select(t => new { tipo = t.Key, cantidad = t.Count() })
                        }).OrderBy(x => x.fecha);
                    resultado = tendenciasDia;
                    break;
            }

            return Ok(new
            {
                fechaInicio,
                fechaFin,
                agrupacion,
                tendencias = resultado
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en tendencias: {ex.Message}");
            return StatusCode(500, new { mensaje = "Error calculando tendencias" });
        }
    }

    private static int GetWeekOfYear(DateTime fecha)
    {
        var cultura = System.Globalization.CultureInfo.CurrentCulture;
        return cultura.Calendar.GetWeekOfYear(fecha,
            System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
}