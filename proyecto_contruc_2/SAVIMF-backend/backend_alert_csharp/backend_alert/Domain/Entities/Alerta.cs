public enum AlertaEstado
{
    Disponible,
    Tomada,
    Llegada,     // 🔥 Detectado por GPS automáticamente (≤30m)
    Atendida,    // 🔥 Requiere atestado completado
    Resuelto,
    NoAtendida,
    Vencida,     // 🔥 Alerta que cumplió tiempo sin respuesta
    NoResuelta   // 🔥 NUEVO: Alerta que fue tomada pero víctima reactivó >10min (no se atendió a tiempo)
}

public class Alerta
{
    // 🆔 ID del documento de Firestore (se asigna después de leer de BD)
    public string Id { get; set; } = "";

    // 📋 Campos obligatorios (siempre llenos)
    public string DevEUI { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double Bateria { get; set; }
    public DateTime Timestamp { get; set; }  // ⏰ Se actualiza con cada evento
    public DateTime FechaCreacion { get; set; }  // 🏁 NO se actualiza, solo primera vez
    public string DeviceId { get; set; }
    public string NombreVictima { get; set; }

    // 🔥 NUEVOS CAMPOS PARA SISTEMA DE PRIORIDADES
    public int CantidadActivaciones { get; set; } = 1;
    public DateTime UltimaActivacion { get; set; }
    public string NivelUrgencia { get; set; } = "baja"; // baja, media, critica
    public bool EsRecurrente { get; set; } = false;

    // 🆕 Campos opcionales (se llenan después)
    public string Estado { get; set; }
    public DateTime? FechaLlegada { get; set; }    // 🔥 NUEVO: GPS detecta llegada
    public DateTime? FechaAtendida { get; set; }   // 🔥 NUEVO: Atestado completado
    public DateTime? FechaResuelto { get; set; }
    public DateTime? FechaTomada { get; set; }
    public string PatrulleroAsignado { get; set; }

    public Alerta(string devEUI, double lat, double lon, double bateria, DateTime timestamp, string deviceId, string nombreVictima)
    {
        // Campos obligatorios
        DevEUI = devEUI;
        Lat = lat;
        Lon = lon;
        Bateria = bateria;
        Timestamp = timestamp;
        FechaCreacion = timestamp;  // 🏁 Se asigna solo en la creación inicial
        DeviceId = deviceId;
        NombreVictima = nombreVictima;

        // � INICIALIZACIÓN NUEVOS CAMPOS
        CantidadActivaciones = 1;
        UltimaActivacion = timestamp;
        NivelUrgencia = "baja";
        EsRecurrente = false;

        // 🔧 Valores por defecto para campos opcionales
        Estado = "disponible";
        FechaLlegada = null;
        FechaAtendida = null;
        FechaResuelto = null;
        FechaTomada = null;
        PatrulleroAsignado = "";
    }

    // 🆕 Constructor adicional para cuando se leen datos completos de la BD
    public Alerta(string devEUI, double lat, double lon, double bateria, DateTime timestamp, DateTime fechaCreacion,
                  string deviceId, string nombreVictima, string estado, DateTime? fechaLlegada, DateTime? fechaAtendida,
                  DateTime? fechaResuelto, DateTime? fechaTomada, string patrulleroAsignado,
                  int cantidadActivaciones = 1, DateTime? ultimaActivacion = null, string nivelUrgencia = "baja", bool esRecurrente = false)
    {
        DevEUI = devEUI;
        Lat = lat;
        Lon = lon;
        Bateria = bateria;
        Timestamp = timestamp;
        FechaCreacion = fechaCreacion;
        DeviceId = deviceId;
        NombreVictima = nombreVictima;
        Estado = estado;
        FechaLlegada = fechaLlegada;
        FechaAtendida = fechaAtendida;
        FechaResuelto = fechaResuelto;
        FechaTomada = fechaTomada;
        PatrulleroAsignado = patrulleroAsignado;

        // 🔥 NUEVOS CAMPOS
        CantidadActivaciones = cantidadActivaciones;
        UltimaActivacion = ultimaActivacion ?? timestamp;
        NivelUrgencia = nivelUrgencia;
        EsRecurrente = esRecurrente;
    }

    // 🎯 MÉTODO PARA CALCULAR NIVEL DE URGENCIA
    public string CalcularNivelUrgencia()
    {
        if (CantidadActivaciones >= 4) return "critica";
        if (CantidadActivaciones >= 2) return "media";
        return "baja";
    }

    // ⏰ MÉTODO PARA VERIFICAR SI DEBE ARCHIVARSE (5+ horas sin atender)
    public bool DebeArchivarse()
    {
        var tiempoLimite = FechaCreacion.AddHours(5);
        return DateTime.UtcNow > tiempoLimite && Estado == "disponible";
    }

    // 🕒 MÉTODO PARA VERIFICAR SI ESTÁ DENTRO DEL RANGO DE 10 MINUTOS
    public bool EstaDentroDelRango()
    {
        var tiempoLimite = UltimaActivacion.AddMinutes(10);
        return DateTime.UtcNow <= tiempoLimite;
    }

    // 🔥 MÉTODO PARA INCREMENTAR ACTIVACIONES
    public void IncrementarActivacion()
    {
        CantidadActivaciones++;
        UltimaActivacion = DateTime.UtcNow;
        Timestamp = DateTime.UtcNow; // Actualizar timestamp también

        // Actualizar nivel de urgencia automáticamente
        NivelUrgencia = CalcularNivelUrgencia();

        // Marcar como recurrente si se activa más de una vez
        if (CantidadActivaciones > 1)
        {
            EsRecurrente = true;
        }
    }

    // ❌ MÉTODO PARA MARCAR COMO VENCIDA (cuando se crea nueva alerta del mismo dispositivo)
    public void MarcarComoVencida()
    {
        Estado = "vencida";
    }

    // 🎯 MÉTODO PARA OBTENER ESTADO COMO ENUM
    public AlertaEstado EstadoAlerta
    {
        get
        {
            return Estado.ToLower() switch
            {
                "disponible" => AlertaEstado.Disponible,
                "tomada" => AlertaEstado.Tomada,
                "llegada" => AlertaEstado.Llegada,
                "atendida" => AlertaEstado.Atendida,
                "resuelto" => AlertaEstado.Resuelto,
                "noatendida" => AlertaEstado.NoAtendida,
                "vencida" => AlertaEstado.Vencida,
                "no_resuelta" => AlertaEstado.NoResuelta,
                _ => AlertaEstado.Disponible
            };
        }
    }

    // 📊 MÉTODO PARA CONVERTIR ALERTA EXPIRADA A OPENDATA (sin atestado policial)
    /// <summary>
    /// Convierte una alerta expirada (vencida/no_resuelta) a OpenData con información limitada
    /// Se usa cuando la alerta NO fue resuelta completamente (no hay atestado policial)
    /// </summary>
    public backend_alert.Domain.Entities.OpenDataIncidente ConvertirAOpenDataSinAtestado()
    {
        // Calcular tiempos si existen fechas
        double? tiempoRespuesta = FechaTomada.HasValue 
            ? (FechaTomada.Value - FechaCreacion).TotalMinutes 
            : null;
        
        double? tiempoLlegada = (FechaTomada.HasValue && FechaLlegada.HasValue)
            ? (FechaLlegada.Value - FechaTomada.Value).TotalMinutes
            : null;

        // Determinar razón de no resolución
        string razonNoResolucion = Estado switch
        {
            "vencida" => "sin_patrullero_disponible",
            "no_resuelta" when FechaLlegada.HasValue => "atestado_incompleto",
            "no_resuelta" when FechaTomada.HasValue => "no_llego_a_tiempo",
            "no_resuelta" => "tiempo_expirado",
            _ => "desconocido"
        };

        return new backend_alert.Domain.Entities.OpenDataIncidente(
            id: Guid.NewGuid().ToString(),
            fechaCreacionAlerta: FechaCreacion,
            fechaTomadaAlerta: FechaTomada,
            fechaLlegada: FechaLlegada,
            fechaAtendida: FechaAtendida,
            fechaResuelto: null, // No se resolvió
            tiempoRespuestaMinutos: tiempoRespuesta,
            tiempoLlegadaMinutos: tiempoLlegada,
            tiempoAtencionMinutos: null,
            tiempoTotalMinutos: null,
            fechaIncidente: FechaCreacion,
            cantidadActivaciones: CantidadActivaciones,
            esRecurrente: EsRecurrente,
            estadoFinal: Estado, // "vencida" o "no_resuelta"
            nivelUrgencia: NivelUrgencia,
            latitudRedondeada: Math.Round(Lat, 3), // 3 decimales ≈ 111 metros
            longitudRedondeada: Math.Round(Lon, 3),
            distrito: ObtenerDistritoAproximado(),
            tipoViolencia: "no_especificado", // No hay atestado
            nivelRiesgo: "no_evaluado",
            alertaVeridica: false, // No se pudo verificar
            edadVictimaRango: "no_especificado",
            generoVictima: "no_especificado",
            requirioAmbulancia: false,
            requirioRefuerzo: false,
            victimaTrasladadaComisaria: false,
            bateriaNivel: (int)Bateria,
            dispositivoTipo: "boton_panico",
            fechaRegistroOpenData: DateTime.UtcNow,
            razonNoResolucion: razonNoResolucion // 🔥 NUEVO
        );
    }

    // 🗺️ MÉTODO AUXILIAR PARA OBTENER DISTRITO (puedes mejorarlo con geocoding real)
    private string ObtenerDistritoAproximado()
    {
        // TODO: Implementar geocoding reverso real si es necesario
        // Por ahora retorna distrito genérico basado en coordenadas de Tacna
        if (Lat >= -18.02 && Lat <= -17.98 && Lon >= -70.26 && Lon <= -70.22)
            return "Alto de la Alianza";
        else if (Lat >= -18.05 && Lat <= -18.00 && Lon >= -70.28 && Lon <= -70.24)
            return "Ciudad Nueva";
        else if (Lat >= -18.01 && Lat <= -17.97 && Lon >= -70.25 && Lon <= -70.23)
            return "Cercado";
        else
            return "Tacna"; // Distrito genérico
    }
}
