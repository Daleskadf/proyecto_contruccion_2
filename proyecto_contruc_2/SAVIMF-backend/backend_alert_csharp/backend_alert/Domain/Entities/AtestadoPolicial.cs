using Google.Cloud.Firestore;
using Domain.Entities; // Para acceder a Alerta cuando esté disponible

namespace backend_alert.Domain.Entities;

/// <summary>
/// Entidad de dominio para Atestado Policial
/// </summary>
public class AtestadoPolicial
{
    // Identificadores
    public string Id { get; set; } = string.Empty;
    public string AlertaId { get; set; } = string.Empty;
    public string PatrulleroUid { get; set; } = string.Empty;
    public string PatrulleroNombre { get; set; } = string.Empty;

    // Fechas en Timestamp Firestore
    public Timestamp FechaCreacion { get; set; }
    public Timestamp FechaIncidente { get; set; }

    // Ubicación
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string Distrito { get; set; } = string.Empty;
    public string DireccionReferencial { get; set; } = string.Empty;

    // Información del incidente
    public string TipoViolencia { get; set; } = string.Empty;
    public string NivelRiesgo { get; set; } = string.Empty;
    public bool AlertaVeridica { get; set; }
    public string DescripcionHechos { get; set; } = string.Empty;

    // Datos víctima
    public string NombreVictima { get; set; } = string.Empty;
    public string DniVictima { get; set; } = string.Empty;
    public int EdadAproximada { get; set; }

    // Acciones
    public bool RequirioAmbulancia { get; set; }
    public bool RequirioRefuerzo { get; set; }
    public bool VictimaTrasladadaComisaria { get; set; }
    public string AccionesRealizadas { get; set; } = string.Empty;

    public string Observaciones { get; set; } = string.Empty;

    // Constructor principal
    public AtestadoPolicial(
        string alertaId,
        string patrulleroUid,
        string patrulleroNombre,
        Timestamp fechaIncidente,
        double latitud,
        double longitud,
        string distrito,
        string tipoViolencia,
        string nivelRiesgo,
        bool alertaVeridica,
        string descripcionHechos,
        string nombreVictima,
        string dniVictima,
        int edadAproximada)
    {
        AlertaId = alertaId;
        PatrulleroUid = patrulleroUid;
        PatrulleroNombre = patrulleroNombre;
        FechaCreacion = Timestamp.GetCurrentTimestamp();
        FechaIncidente = fechaIncidente;
        Latitud = latitud;
        Longitud = longitud;
        Distrito = distrito;
        TipoViolencia = tipoViolencia;
        NivelRiesgo = nivelRiesgo;
        AlertaVeridica = alertaVeridica;
        DescripcionHechos = descripcionHechos;
        NombreVictima = nombreVictima;
        DniVictima = dniVictima;
        EdadAproximada = edadAproximada;
    }

    // Constructor completo para BD
    public AtestadoPolicial(
        string id,
        string alertaId,
        string patrulleroUid,
        string patrulleroNombre,
        Timestamp fechaCreacion,
        Timestamp fechaIncidente,
        double latitud,
        double longitud,
        string distrito,
        string direccionReferencial,
        string tipoViolencia,
        string nivelRiesgo,
        bool alertaVeridica,
        string descripcionHechos,
        string nombreVictima,
        string dniVictima,
        int edadAproximada,
        bool requirioAmbulancia,
        bool requirioRefuerzo,
        bool victimaTrasladadaComisaria,
        string accionesRealizadas,
        string observaciones)
    {
        Id = id;
        AlertaId = alertaId;
        PatrulleroUid = patrulleroUid;
        PatrulleroNombre = patrulleroNombre;
        FechaCreacion = fechaCreacion;
        FechaIncidente = fechaIncidente;
        Latitud = latitud;
        Longitud = longitud;
        Distrito = distrito;
        DireccionReferencial = direccionReferencial;
        TipoViolencia = tipoViolencia;
        NivelRiesgo = nivelRiesgo;
        AlertaVeridica = alertaVeridica;
        DescripcionHechos = descripcionHechos;
        NombreVictima = nombreVictima;
        DniVictima = dniVictima;
        EdadAproximada = edadAproximada;
        RequirioAmbulancia = requirioAmbulancia;
        RequirioRefuerzo = requirioRefuerzo;
        VictimaTrasladadaComisaria = victimaTrasladadaComisaria;
        AccionesRealizadas = accionesRealizadas;
        Observaciones = observaciones;
    }

    public bool EsValido()
    {
        return !string.IsNullOrWhiteSpace(AlertaId)
            && !string.IsNullOrWhiteSpace(PatrulleroUid)
            && !string.IsNullOrWhiteSpace(TipoViolencia)
            && !string.IsNullOrWhiteSpace(NivelRiesgo)
            && !string.IsNullOrWhiteSpace(Distrito)
            && Latitud != 0
            && Longitud != 0;
    }

    // Cambiar el tipo de retorno a OpenDataIncidente
    public OpenDataIncidente ConvertirAOpenData(Alerta? alerta = null)
    {
        // Preferir datos de la alerta si están disponibles (tienen timestamps y metadatos)
        DateTime? fechaCreacionAlerta = alerta?.FechaCreacion ?? this.FechaCreacion.ToDateTime();

        // 🔥 Si falta fechaTomada/fechaLlegada/fechaAtendida en la alerta, aplicar fallbacks razonables:
        DateTime? fechaTomadaAlerta = alerta?.FechaTomada ?? fechaCreacionAlerta;
        DateTime? fechaLlegadaAlerta = alerta?.FechaLlegada ?? fechaTomadaAlerta;
        DateTime? fechaAtendidaAlerta = alerta?.FechaAtendida ?? fechaLlegadaAlerta;

        // fechaResuelto preferirla de alerta, si no existe usar fallback
        DateTime? fechaResuelto = alerta?.FechaResuelto ?? fechaAtendidaAlerta;

        DateTime fechaIncidente = this.FechaIncidente.ToDateTime();

        // Rango de edad anonimizados
        string edadRango;
        if (EdadAproximada <= 0) edadRango = "desconocido";
        else if (EdadAproximada < 18) edadRango = "menor";
        else if (EdadAproximada <= 29) edadRango = "18-29";
        else if (EdadAproximada <= 44) edadRango = "30-44";
        else if (EdadAproximada <= 59) edadRango = "45-59";
        else edadRango = "60+";

        double latRed = Math.Round(this.Latitud, 3);
        double lonRed = Math.Round(this.Longitud, 3);

    int cantidadActivaciones = alerta?.CantidadActivaciones ?? 1;
    bool esRecurrente = alerta?.EsRecurrente ?? false;
    string estadoFinal = !string.IsNullOrWhiteSpace(alerta?.Estado) ? alerta!.Estado : "resuelto";
    string nivelUrgencia = !string.IsNullOrWhiteSpace(alerta?.NivelUrgencia) ? alerta!.NivelUrgencia : (this.NivelRiesgo ?? "baja");
    int? bateriaNivel = alerta != null ? (int?)Convert.ToInt32(alerta.Bateria) : 0;
    string dispositivoTipo = !string.IsNullOrWhiteSpace(alerta?.DeviceId) ? alerta!.DeviceId : "boton_panico";

    // Asegurar que no haya nulls inesperados en fechas usadas por OpenData
    fechaCreacionAlerta ??= this.FechaCreacion.ToDateTime();
    fechaTomadaAlerta ??= fechaCreacionAlerta;
    fechaLlegadaAlerta ??= fechaTomadaAlerta;
    fechaAtendidaAlerta ??= fechaLlegadaAlerta;
    fechaResuelto ??= fechaAtendidaAlerta;

        return OpenDataIncidente.CalcularTiempos(
            id: this.Id ?? string.Empty,
            fechaCreacionAlerta: fechaCreacionAlerta,
            fechaTomadaAlerta: fechaTomadaAlerta,
            fechaLlegada: fechaLlegadaAlerta,
            fechaAtendida: fechaAtendidaAlerta,
            fechaResuelto: fechaResuelto,
            fechaIncidente: fechaIncidente,
            cantidadActivaciones: cantidadActivaciones,
            esRecurrente: esRecurrente,
            estadoFinal: estadoFinal,
            nivelUrgencia: nivelUrgencia,
            latitudRedondeada: latRed,
            longitudRedondeada: lonRed,
            distrito: this.Distrito ?? string.Empty,
            tipoViolencia: this.TipoViolencia ?? string.Empty,
            nivelRiesgo: this.NivelRiesgo ?? string.Empty,
            alertaVeridica: this.AlertaVeridica,
            edadVictimaRango: edadRango,
            generoVictima: string.Empty,
            requirioAmbulancia: this.RequirioAmbulancia,
            requirioRefuerzo: this.RequirioRefuerzo,
            victimaTrasladadaComisaria: this.VictimaTrasladadaComisaria,
            bateriaNivel: bateriaNivel,
            dispositivoTipo: dispositivoTipo
        );
    }
}
