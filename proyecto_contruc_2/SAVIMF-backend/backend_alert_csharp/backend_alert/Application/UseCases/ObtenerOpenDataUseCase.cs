using backend_alert.Domain.Entities;
using backend_alert.Domain.Interfaces;

namespace backend_alert.Application.UseCases;

/// <summary>
/// Caso de uso para obtener datos de Open Data
/// Pattern: Use Case (Clean Architecture) + Query (CQRS)
/// </summary>
public class ObtenerOpenDataUseCase
{
    private readonly IOpenDataRepository _openDataRepository;
    private readonly ILogger<ObtenerOpenDataUseCase> _logger;

    public ObtenerOpenDataUseCase(
        IOpenDataRepository openDataRepository,
        ILogger<ObtenerOpenDataUseCase> logger)
    {
        _openDataRepository = openDataRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene incidentes individuales por periodo
    /// </summary>
    public async Task<List<OpenDataIncidente>> ObtenerIncidentesPorPeriodoAsync(int anio, int? mes = null)
    {
        try
        {
            _logger.LogInformation("Obteniendo incidentes de Open Data para {Anio}-{Mes}", anio, mes ?? 0);
            var incidentes = await _openDataRepository.ObtenerIncidentesPorPeriodoAsync(anio, mes);
            
            _logger.LogInformation("Se encontraron {Count} incidentes", incidentes.Count);
            return incidentes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener incidentes de Open Data");
            throw;
        }
    }

    /// <summary>
    /// Obtiene incidentes de un distrito específico
    /// </summary>
    public async Task<List<OpenDataIncidente>> ObtenerIncidentesPorDistritoAsync(string distrito)
    {
        try
        {
            _logger.LogInformation("Obteniendo incidentes de Open Data para distrito {Distrito}", distrito);
            return await _openDataRepository.ObtenerIncidentesPorDistritoAsync(distrito);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener incidentes por distrito");
            throw;
        }
    }

    /// <summary>
    /// Obtiene estadísticas agregadas por distrito y periodo
    /// </summary>
    public async Task<OpenDataAgregado?> ObtenerEstadisticasAgregadasAsync(string distrito, int anio, int mes)
    {
        try
        {
            _logger.LogInformation("Obteniendo estadísticas agregadas para {Distrito} {Anio}-{Mes}", 
                distrito, anio, mes);
            return await _openDataRepository.ObtenerAgregadoAsync(distrito, anio, mes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas agregadas");
            throw;
        }
    }

    /// <summary>
    /// Obtiene todas las estadísticas agregadas de un año
    /// </summary>
    public async Task<List<OpenDataAgregado>> ObtenerEstadisticasAnualesAsync(int anio)
    {
        try
        {
            _logger.LogInformation("Obteniendo estadísticas anuales para {Anio}", anio);
            return await _openDataRepository.ObtenerAgregadosPorAnioAsync(anio);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas anuales");
            throw;
        }
    }

    /// <summary>
    /// Obtiene estadísticas globales para dashboard público
    /// </summary>
    public async Task<Dictionary<string, object>> ObtenerEstadisticasGlobalesAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo estadísticas globales de Open Data");
            return await _openDataRepository.ObtenerEstadisticasGlobalesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estadísticas globales");
            throw;
        }
    }
}
