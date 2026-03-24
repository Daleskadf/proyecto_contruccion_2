namespace backend_alert.Domain.Entities;

/// <summary>
/// Reporte de análisis geoespacial por zonas/distritos
/// Incluye hotspots, distribución temporal, tipos de violencia predominantes
/// </summary>
public class ReporteZona
{
    public string Distrito { get; init; } = string.Empty;
    
    // 📅 Periodo
    public DateTime FechaInicio { get; init; }
    public DateTime FechaFin { get; init; }
    
    // 📊 Totales
    public int TotalAlertas { get; init; }
    public int AlertasVeridicas { get; init; }
    public double TasaVeracidad { get; init; }
    
    // 🔥 Hotspots (zonas críticas)
    public List<Hotspot> Hotspots { get; init; } = new();
    
    // 🔍 Tipo de violencia predominante
    public string TipoViolenciaMasComun { get; init; } = string.Empty;
    public Dictionary<string, int> DistribucionTipoViolencia { get; init; } = new();
    
    // ⚠️ Nivel de riesgo predominante
    public string RiesgoPredominante { get; init; } = string.Empty;
    public Dictionary<string, int> DistribucionRiesgo { get; init; } = new();
    
    // 📅 Horas pico
    public List<int> HorasPico { get; init; } = new();
    public Dictionary<int, int> AlertasPorHora { get; init; } = new();
    
    // 📍 Ubicación central
    public double LatitudCentral { get; init; }
    public double LongitudCentral { get; init; }
    
    // ⏱️ Tiempos promedio
    public double TiempoPromedioRespuesta { get; init; }
    public double TiempoPromedioResolucion { get; init; }
    
    // 👥 Demografía (anonimizada)
    public Dictionary<string, int> DistribucionEdades { get; init; } = new();
    
    // 🚨 Recursos necesarios
    public int CasosConAmbulancia { get; init; }
    public int CasosConRefuerzo { get; init; }
}

/// <summary>
/// Representa un punto crítico (hotspot) en el mapa
/// </summary>
public class Hotspot
{
    public double Latitud { get; init; }
    public double Longitud { get; init; }
    public int Radio { get; init; } // en metros
    public int CantidadAlertas { get; init; }
    public string NivelPeligrosidad { get; init; } = string.Empty; // bajo, medio, alto, critico
}
