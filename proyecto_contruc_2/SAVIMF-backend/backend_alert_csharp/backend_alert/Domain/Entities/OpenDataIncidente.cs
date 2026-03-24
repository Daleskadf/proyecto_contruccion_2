namespace backend_alert.Domain.Entities;

/// <summary>
/// Entidad de dominio para Open Data - Datos públicos anónimos de incidentes COMPLETOS
/// INCLUYE: Todos los tiempos, activaciones, batería, estado - TODO el ciclo de vida de la alerta
/// NO INCLUYE: Nombres, DNI, DevEUI, emails, coordenadas exactas, datos personales sensibles
/// Pattern: Value Object (DDD) - inmutable, sin identidad propia
/// </summary>
public class OpenDataIncidente
{
    // 🆔 ID del incidente (para rastreo sin exponer datos sensibles)
    public string Id { get; init; } = string.Empty;

    // ⏰ TIEMPOS DEL CICLO DE VIDA DE LA ALERTA (todo anonimizado)
    public DateTime? FechaCreacionAlerta { get; init; }      // Cuando se creó la alerta
    public DateTime? FechaTomadaAlerta { get; init; }        // Cuando operador asignó patrullero
    public DateTime? FechaLlegada { get; init; }             // 🔥 NUEVO: Cuando patrullero llegó (GPS auto)
    public DateTime? FechaAtendida { get; init; }            // 🔥 NUEVO: Cuando completó atestado
    public DateTime? FechaResuelto { get; init; }            // Cuando se resolvió
    
    // 📊 TIEMPOS CALCULADOS (en minutos)
    public double? TiempoRespuestaMinutos { get; init; }     // FechaTomada - FechaCreacion
    public double? TiempoLlegadaMinutos { get; init; }       // 🔥 NUEVO: FechaLlegada - FechaTomada
    public double? TiempoAtencionMinutos { get; init; }      // 🔥 NUEVO: FechaAtendida - FechaLlegada
    public double? TiempoTotalMinutos { get; init; }         // FechaResuelto - FechaCreacion

    // 📅 Información temporal del incidente (para análisis estadístico)
    public DateTime FechaIncidente { get; init; }
    public int Anio => FechaIncidente.Year;
    public int Mes => FechaIncidente.Month;
    public int Dia => FechaIncidente.Day;
    public string MesNombre => FechaIncidente.ToString("MMMM");
    public int DiaSemana => (int)FechaIncidente.DayOfWeek;
    public string DiaSemanaNombre => FechaIncidente.ToString("dddd");
    public int HoraDelDia => FechaIncidente.Hour;
    public int MinutoDelDia => FechaIncidente.Minute;

    // 📊 METADATA DE LA ALERTA
    public int CantidadActivaciones { get; init; }           // Cuántas veces se activó el botón
    public bool EsRecurrente { get; init; }                  // Si la víctima tiene alertas previas
    public string EstadoFinal { get; init; } = string.Empty; // "resuelto", "vencida", "no_resuelta"
    public string NivelUrgencia { get; init; } = string.Empty; // "baja", "media", "alta", "critica"
    
    // 🚫 Razón de no resolución (solo para alertas vencidas/no_resueltas, null si está resuelta)
    public string? RazonNoResolucion { get; init; } // "sin_patrullero_disponible", "no_llego_a_tiempo", "atestado_incompleto", "tiempo_expirado"

    // 📍 Ubicación anonimizada (redondeada a 3 decimales ≈ 111 metros)
    public double LatitudRedondeada { get; init; }
    public double LongitudRedondeada { get; init; }
    public string Distrito { get; init; } = string.Empty;

    // 🔍 Características del incidente (del atestado policial)
    public string TipoViolencia { get; init; } = string.Empty;
    public string NivelRiesgo { get; init; } = string.Empty;
    public bool AlertaVeridica { get; init; }

    // 👤 Datos demográficos anonimizados
    public string EdadVictimaRango { get; init; } = string.Empty; // "menor", "18-29", "30-44", "45-59", "60+"
    public string GeneroVictima { get; init; } = string.Empty;    // "femenino", "masculino", "otro" (si se captura)

    // 🚨 Recursos movilizados
    public bool RequirioAmbulancia { get; init; }
    public bool RequirioRefuerzo { get; init; }
    public bool VictimaTrasladadaComisaria { get; init; }

    // 🔋 DATOS DEL DISPOSITIVO (anonimizados - sin DevEUI, AppKey, JoinEui)
    public int? BateriaNivel { get; init; }                   // Nivel de batería 0-100
    public string DispositivoTipo { get; init; } = string.Empty; // "boton_panico", "dispositivo_iot" (genérico)
    
    // 📅 Metadata del registro
    public DateTime FechaRegistroOpenData { get; init; }

    public OpenDataIncidente(
        string id,
        DateTime? fechaCreacionAlerta,
        DateTime? fechaTomadaAlerta,
        DateTime? fechaLlegada,
        DateTime? fechaAtendida,
        DateTime? fechaResuelto,
        double? tiempoRespuestaMinutos,
        double? tiempoLlegadaMinutos,
        double? tiempoAtencionMinutos,
        double? tiempoTotalMinutos,
        DateTime fechaIncidente,
        int cantidadActivaciones,
        bool esRecurrente,
        string estadoFinal,
        string nivelUrgencia,
        double latitudRedondeada,
        double longitudRedondeada,
        string distrito,
        string tipoViolencia,
        string nivelRiesgo,
        bool alertaVeridica,
        string edadVictimaRango,
        string generoVictima,
        bool requirioAmbulancia,
        bool requirioRefuerzo,
        bool victimaTrasladadaComisaria,
        int? bateriaNivel,
        string dispositivoTipo,
        DateTime fechaRegistroOpenData,
        string? razonNoResolucion = null)  // 🔥 NUEVO parámetro opcional
    {
        Id = id;
        FechaCreacionAlerta = fechaCreacionAlerta;
        FechaTomadaAlerta = fechaTomadaAlerta;
        FechaLlegada = fechaLlegada;
        FechaAtendida = fechaAtendida;
        FechaResuelto = fechaResuelto;
        TiempoRespuestaMinutos = tiempoRespuestaMinutos;
        TiempoLlegadaMinutos = tiempoLlegadaMinutos;
        TiempoAtencionMinutos = tiempoAtencionMinutos;
        TiempoTotalMinutos = tiempoTotalMinutos;
        FechaIncidente = fechaIncidente;
        CantidadActivaciones = cantidadActivaciones;
        EsRecurrente = esRecurrente;
        EstadoFinal = estadoFinal;
        NivelUrgencia = nivelUrgencia;
        LatitudRedondeada = latitudRedondeada;
        LongitudRedondeada = longitudRedondeada;
        Distrito = distrito;
        TipoViolencia = tipoViolencia;
        NivelRiesgo = nivelRiesgo;
        AlertaVeridica = alertaVeridica;
        EdadVictimaRango = edadVictimaRango;
        GeneroVictima = generoVictima;
        RequirioAmbulancia = requirioAmbulancia;
        RequirioRefuerzo = requirioRefuerzo;
        VictimaTrasladadaComisaria = victimaTrasladadaComisaria;
        BateriaNivel = bateriaNivel;
        DispositivoTipo = dispositivoTipo;
        FechaRegistroOpenData = fechaRegistroOpenData;
        RazonNoResolucion = razonNoResolucion;  // 🔥 NUEVO
    }

    /// <summary>
    /// Calcula tiempos si las fechas están disponibles
    /// </summary>
    public static OpenDataIncidente CalcularTiempos(
        string id,
        DateTime? fechaCreacionAlerta,
        DateTime? fechaTomadaAlerta,
        DateTime? fechaLlegada,
        DateTime? fechaAtendida,
        DateTime? fechaResuelto,
        DateTime fechaIncidente,
        int cantidadActivaciones,
        bool esRecurrente,
        string estadoFinal,
        string nivelUrgencia,
        double latitudRedondeada,
        double longitudRedondeada,
        string distrito,
        string tipoViolencia,
        string nivelRiesgo,
        bool alertaVeridica,
        string edadVictimaRango,
        string generoVictima,
        bool requirioAmbulancia,
        bool requirioRefuerzo,
        bool victimaTrasladadaComisaria,
        int? bateriaNivel,
        string dispositivoTipo)
    {
        double? tiempoRespuesta = null;
        double? tiempoLlegadaCalc = null;
        double? tiempoAtencion = null;
        double? tiempoTotal = null;

        if (fechaCreacionAlerta.HasValue && fechaTomadaAlerta.HasValue)
            tiempoRespuesta = (fechaTomadaAlerta.Value - fechaCreacionAlerta.Value).TotalMinutes;

        if (fechaTomadaAlerta.HasValue && fechaLlegada.HasValue)
            tiempoLlegadaCalc = (fechaLlegada.Value - fechaTomadaAlerta.Value).TotalMinutes;

        if (fechaLlegada.HasValue && fechaAtendida.HasValue)
            tiempoAtencion = (fechaAtendida.Value - fechaLlegada.Value).TotalMinutes;

        if (fechaCreacionAlerta.HasValue && fechaResuelto.HasValue)
            tiempoTotal = (fechaResuelto.Value - fechaCreacionAlerta.Value).TotalMinutes;

        return new OpenDataIncidente(
            id,
            fechaCreacionAlerta,
            fechaTomadaAlerta,
            fechaLlegada,
            fechaAtendida,
            fechaResuelto,
            tiempoRespuesta,
            tiempoLlegadaCalc,
            tiempoAtencion,
            tiempoTotal,
            fechaIncidente,
            cantidadActivaciones,
            esRecurrente,
            estadoFinal,
            nivelUrgencia,
            latitudRedondeada,
            longitudRedondeada,
            distrito,
            tipoViolencia,
            nivelRiesgo,
            alertaVeridica,
            edadVictimaRango,
            generoVictima,
            requirioAmbulancia,
            requirioRefuerzo,
            victimaTrasladadaComisaria,
            bateriaNivel,
            dispositivoTipo,
            DateTime.UtcNow
        );
    }
}
