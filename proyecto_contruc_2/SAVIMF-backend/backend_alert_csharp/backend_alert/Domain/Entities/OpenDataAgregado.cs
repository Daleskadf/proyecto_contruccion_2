namespace backend_alert.Domain.Entities;

/// <summary>
/// Value Object para estadísticas agregadas de Open Data
/// Representa métricas calculadas por distrito/periodo sin datos individuales
/// Pattern: Value Object + Aggregate Root
/// </summary>
public class OpenDataAgregado
{
    // 🆔 Identificación del agregado
    public string Id { get; set; } = string.Empty;
    public string Distrito { get; init; } = string.Empty;
    public int Anio { get; init; }
    public int Mes { get; init; }

    // 📊 Estadísticas de incidentes
    public int TotalIncidentes { get; set; }
    public int IncidentesVeridicos { get; set; }
    public int IncidentesFalsos => TotalIncidentes - IncidentesVeridicos;
    public double TasaVeracidad => TotalIncidentes > 0 ? (double)IncidentesVeridicos / TotalIncidentes * 100 : 0;

    // 🔍 Por tipo de violencia
    public int ViolenciaFisica { get; set; }
    public int ViolenciaPsicologica { get; set; }
    public int ViolenciaSexual { get; set; }
    public int ViolenciaEconomica { get; set; }

    // ⚠️ Por nivel de riesgo
    public int RiesgoBajo { get; set; }
    public int RiesgoMedio { get; set; }
    public int RiesgoAlto { get; set; }
    public int RiesgoCritico { get; set; }

    // 👥 Distribución por edad de víctimas
    public int VictimasMenores { get; set; }
    public int Victimas18_29 { get; set; }
    public int Victimas30_44 { get; set; }
    public int Victimas45_59 { get; set; }
    public int Victimas60Plus { get; set; }

    // 🚨 Recursos movilizados
    public int CasosConAmbulancia { get; set; }
    public int CasosConRefuerzo { get; set; }

    // ⏱️ Distribución temporal
    public Dictionary<string, int> IncidentesPorDia { get; set; } = new();
    public Dictionary<int, int> IncidentesPorHora { get; set; } = new();

    // 📍 Punto central del distrito (para visualización)
    public double LatitudCentral { get; set; }
    public double LongitudCentral { get; set; }

    // 📅 Metadata
    public DateTime FechaActualizacion { get; set; }

    // Constructor
    public OpenDataAgregado(string distrito, int anio, int mes)
    {
        Distrito = distrito;
        Anio = anio;
        Mes = mes;
        FechaActualizacion = DateTime.UtcNow;
        IncidentesPorDia = new Dictionary<string, int>();
        IncidentesPorHora = new Dictionary<int, int>();
    }

    /// <summary>
    /// Agrega un incidente a las estadísticas
    /// Pattern: Aggregate Root - maneja la consistencia interna
    /// </summary>
    public void AgregarIncidente(OpenDataIncidente incidente)
    {
        TotalIncidentes++;
        
        if (incidente.AlertaVeridica)
            IncidentesVeridicos++;

        // Tipo de violencia
        switch (incidente.TipoViolencia.ToLower())
        {
            case "fisica": ViolenciaFisica++; break;
            case "psicologica": ViolenciaPsicologica++; break;
            case "sexual": ViolenciaSexual++; break;
            case "economica": ViolenciaEconomica++; break;
        }

        // Nivel de riesgo
        switch (incidente.NivelRiesgo.ToLower())
        {
            case "bajo": RiesgoBajo++; break;
            case "medio": RiesgoMedio++; break;
            case "alto": RiesgoAlto++; break;
            case "critico": RiesgoCritico++; break;
        }

        // Edad víctima
        switch (incidente.EdadVictimaRango)
        {
            case "menor": VictimasMenores++; break;
            case "18-29": Victimas18_29++; break;
            case "30-44": Victimas30_44++; break;
            case "45-59": Victimas45_59++; break;
            case "60+": Victimas60Plus++; break;
        }

        // Recursos
        if (incidente.RequirioAmbulancia) CasosConAmbulancia++;
        if (incidente.RequirioRefuerzo) CasosConRefuerzo++;

        // Distribución temporal
        var diaNombre = incidente.DiaSemanaNombre;
        if (!IncidentesPorDia.ContainsKey(diaNombre))
            IncidentesPorDia[diaNombre] = 0;
        IncidentesPorDia[diaNombre]++;

        var hora = incidente.HoraDelDia;
        if (!IncidentesPorHora.ContainsKey(hora))
            IncidentesPorHora[hora] = 0;
        IncidentesPorHora[hora]++;

        FechaActualizacion = DateTime.UtcNow;
    }

    /// <summary>
    /// Calcula métricas derivadas para análisis
    /// </summary>
    public Dictionary<string, object> ObtenerMetricas()
    {
        return new Dictionary<string, object>
        {
            { "distrito", Distrito },
            { "periodo", $"{Anio}-{Mes:D2}" },
            { "total_incidentes", TotalIncidentes },
            { "tasa_veracidad", Math.Round(TasaVeracidad, 2) },
            { "tipo_mas_frecuente", ObtenerTipoMasFrecuente() },
            { "riesgo_predominante", ObtenerRiesgoPredominante() },
            { "hora_pico", ObtenerHoraPico() },
            { "dia_mas_activo", ObtenerDiaMasActivo() }
        };
    }

    private string ObtenerTipoMasFrecuente()
    {
        var tipos = new Dictionary<string, int>
        {
            { "Física", ViolenciaFisica },
            { "Psicológica", ViolenciaPsicologica },
            { "Sexual", ViolenciaSexual },
            { "Económica", ViolenciaEconomica }
        };
        return tipos.MaxBy(t => t.Value).Key;
    }

    private string ObtenerRiesgoPredominante()
    {
        var riesgos = new Dictionary<string, int>
        {
            { "Bajo", RiesgoBajo },
            { "Medio", RiesgoMedio },
            { "Alto", RiesgoAlto },
            { "Crítico", RiesgoCritico }
        };
        return riesgos.MaxBy(r => r.Value).Key;
    }

    private int ObtenerHoraPico()
    {
        return IncidentesPorHora.Count > 0 
            ? IncidentesPorHora.MaxBy(h => h.Value).Key 
            : 0;
    }

    private string ObtenerDiaMasActivo()
    {
        return IncidentesPorDia.Count > 0 
            ? IncidentesPorDia.MaxBy(d => d.Value).Key 
            : "N/A";
    }
}
