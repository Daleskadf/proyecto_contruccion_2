namespace backend_alert.Domain.Entities;

/// <summary>
/// Reporte de desempeño de patrulleros
/// Incluye métricas de tiempos, alertas atendidas, zonas cubiertas
/// </summary>
public class ReportePatrullero
{
    public string PatrulleroId { get; init; } = string.Empty;
    public string NombrePatrullero { get; init; } = string.Empty;
    public string Dni { get; init; } = string.Empty;
    
    // 📅 Periodo del reporte
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    
    // 📊 Métricas de alertas
    public int TotalAlertasAsignadas { get; init; }
    public int AlertasAtendidas { get; init; }
    public int AlertasCanceladas { get; init; }
    public double TasaCompletacion { get; init; }
    
    // ⏱️ Métricas de tiempo (en minutos)
    public double TiempoPromedioRespuesta { get; init; }
    public double TiempoPromedioResolucion { get; init; }
    public double TiempoPromedioEnCamino { get; init; }
    
    // 📊 Distribución por estado
    public Dictionary<string, int> AlertasPorEstado { get; init; } = new();
    
    // 🚨 Distribución por urgencia
    public Dictionary<string, int> AlertasPorUrgencia { get; init; } = new();
    
    // 📋 Atestados completados
    public int AtestadosCompletados { get; init; }
    public int AlertasVeridicas { get; init; }
    public int AlertasFalsas { get; init; }
    public double TasaVeracidad { get; init; }
    
    // 📍 Zonas/Distritos atendidos
    public Dictionary<string, int> DistritosMasAtendidos { get; init; } = new();
    
    // 📅 Distribución temporal
    public Dictionary<int, int> AlertasPorHora { get; init; } = new();
    public Dictionary<string, int> AlertasPorDia { get; init; } = new();
    
    // 📊 Recursos movilizados
    public int CasosConAmbulancia { get; init; }
    public int CasosConRefuerzo { get; init; }
}
