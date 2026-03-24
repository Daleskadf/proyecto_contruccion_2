using backend_alert.Domain.Entities;

namespace backend_alert.Domain.Interfaces;

/// <summary>
/// Repositorio para gestionar datos públicos (Open Data)
/// Pattern: Repository - gestión de datos agregados y anónimos
/// </summary>
public interface IOpenDataRepository
{
    /// <summary>
    /// Guarda un incidente individual anónimo
    /// </summary>
    Task GuardarIncidenteAsync(OpenDataIncidente incidente);

    /// <summary>
    /// Obtiene todos los incidentes de un periodo
    /// </summary>
    Task<List<OpenDataIncidente>> ObtenerIncidentesPorPeriodoAsync(int anio, int? mes = null);

    /// <summary>
    /// Obtiene incidentes de un distrito específico
    /// </summary>
    Task<List<OpenDataIncidente>> ObtenerIncidentesPorDistritoAsync(string distrito);

    /// <summary>
    /// Guarda o actualiza datos agregados de un distrito/periodo
    /// </summary>
    Task GuardarAgregadoAsync(OpenDataAgregado agregado);

    /// <summary>
    /// Obtiene datos agregados de un distrito y periodo
    /// </summary>
    Task<OpenDataAgregado?> ObtenerAgregadoAsync(string distrito, int anio, int mes);

    /// <summary>
    /// Obtiene todos los datos agregados de un año
    /// </summary>
    Task<List<OpenDataAgregado>> ObtenerAgregadosPorAnioAsync(int anio);

    /// <summary>
    /// Obtiene estadísticas globales para dashboard público
    /// </summary>
    Task<Dictionary<string, object>> ObtenerEstadisticasGlobalesAsync();

    /// <summary>
    /// Regenera datos agregados desde atestados policiales
    /// Útil para recalcular estadísticas cuando hay correcciones
    /// </summary>
    Task RegenerarAgregadosAsync(int anio, int mes);
}
