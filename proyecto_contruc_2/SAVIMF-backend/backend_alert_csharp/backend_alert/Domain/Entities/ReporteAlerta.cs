namespace backend_alert.Domain.Entities;

/// <summary>
/// Reporte de análisis de alertas del sistema
/// Incluye tiempos, recurrencia, estados, distribución geográfica
/// </summary>
public class ReporteAlerta
{
    // 📅 Periodo del reporte
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    
    // 📊 Totales generales
    public int TotalAlertasCreadas { get; init; }
    public int AlertasAtendidas { get; init; }
    public int AlertasPendientes { get; init; }
    public int AlertasCanceladas { get; init; }
    public int AlertasVencidas { get; init; }
    
    // ⏱️ Tiempos promedio (en minutos)
    public double TiempoPromedioRespuesta { get; init; }
    public double TiempoPromedioResolucion { get; init; }
    public double TiempoPromedioTotal { get; init; }
    
    // 🔄 Recurrencia
    public int AlertasRecurrentes { get; init; }
    public int DispositivosMasActivados { get; init; }
    public double PorcentajeRecurrencia { get; init; }
    
    // ✅ Veracidad
    public int AlertasVeridicas { get; init; }
    public int AlertasFalsas { get; init; }
    public double TasaVeracidad { get; init; }
    
    // 🚨 Distribución por urgencia
    public int UrgenciaBaja { get; init; }
    public int UrgenciaMedia { get; init; }
    public int UrgenciaAlta { get; init; }
    public int UrgenciaCritica { get; init; }
    
    // 📊 Activaciones múltiples
    public int AlertasCon1Activacion { get; init; }
    public int AlertasCon2Activaciones { get; init; }
    public int AlertasCon3Activaciones { get; init; }
    public int AlertasCon4MasActivaciones { get; init; }
    
    // 🚑 Recursos movilizados
    public int AmbulanciaSolicitadas { get; init; }
    public int RefuerzosSolicitados { get; init; }
    
    // 📍 Distribución geográfica
    public Dictionary<string, int> AlertasPorDistrito { get; init; } = new();
    
    // 📅 Distribución temporal
    public Dictionary<int, int> AlertasPorHora { get; init; } = new();
    public Dictionary<string, int> AlertasPorDia { get; init; } = new();
    
    // 🔋 Estado de dispositivos
    public double BateriaPromedio { get; init; }
    public int DispositivosBateriaBaja { get; init; } // < 20%
}
