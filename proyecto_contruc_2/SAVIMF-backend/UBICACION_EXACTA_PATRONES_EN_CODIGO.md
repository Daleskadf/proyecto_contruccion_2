# 🎯 UBICACIÓN EXACTA DE PATRONES EN LÍNEAS DE CÓDIGO

## 📊 ÍNDICE POR PATRÓN

- **PATRÓN 1: REPOSITORY PATTERN** → Líneas exactas
- **PATRÓN 2: DATA MAPPER PATTERN** → Líneas exactas
- **PATRÓN 3: STRATEGY PATTERN** → Líneas exactas (semi-aplicado con condicionales)
- **ARQUITECTURA: CLEAN ARCHITECTURE** → Líneas exactas en todos los archivos
- **ARQUITECTURA: USE CASE PATTERN** → Líneas exactas
- **PRINCIPIO: DEPENDENCY INJECTION** → Líneas exactas
- **ENFOQUE: DOMAIN-DRIVEN DESIGN** → Líneas exactas
- **BUENA PRÁCTICA: ENUMS** → Líneas exactas

---

## 🎯 PATRÓN 1: REPOSITORY PATTERN

**Qué es**: Abstracción de acceso a datos mediante interfaz.

### Interfaz: IAlertaRepository.cs

**Ubicación**: `/backend_alert_csharp/backend_alert/Domain/Interfaces/IAlertaRepository.cs`

```csharp
// LÍNEA 7: Definición de interfaz
public interface IAlertaRepository
{
    Task SaveAsync(Alerta alerta);                              // Línea 9
    Task<List<Alerta>> ListarAlertasAsync();                    // Línea 10
    Task<Alerta?> ObtenerPorIdAsync(string alertaId);          // Línea 13
    Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates);  // Línea 16
    Task<Alerta?> BuscarAlertaRecienteAsync(string deviceId, DateTime desde);     // Línea 18
    // ... más métodos
}
```

**Métodos clave implementados en IAlertaRepository**:
- **Línea 9**: `SaveAsync()` - guardar nueva alerta
- **Línea 10**: `ListarAlertasAsync()` - listar todas
- **Línea 16**: `UpdateFieldsAsync()` - actualizar campos
- **Línea 18**: `BuscarAlertaRecienteAsync()` - buscar alerta activa (-10min)

### Implementación: AlertaRepositoryFirestore.cs

**Ubicación**: `/backend_alert_csharp/backend_alert/Infrastructure/Persistence/AlertaRepositoryFirestore.cs`

```csharp
// LÍNEA 10: Implementa interfaz
public class AlertaRepositoryFirestore : IAlertaRepository
{
    // LÍNEA 12: Campo privado Firestore
    private readonly FirestoreDb _firestoreDb;

    // LÍNEA 14: Constructor
    public AlertaRepositoryFirestore(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    // LÍNEA 19: IMPLEMENTACIÓN SaveAsync
    public async Task SaveAsync(Alerta alerta)
    {
        // Línea 21-25: Normalización a UTC
        DateTime utcTimestamp = alerta.Timestamp.Kind == DateTimeKind.Utc
            ? alerta.Timestamp
            : alerta.Timestamp.ToUniversalTime();

        // Línea 27-30: Normalización FechaCreacion
        DateTime utcFechaCreacion = alerta.FechaCreacion.Kind == DateTimeKind.Utc
            ? alerta.FechaCreacion
            : alerta.FechaCreacion.ToUniversalTime();

        // LÍNEA 32: ADD a Firestore collection "alertas"
        await _firestoreDb.Collection("alertas").AddAsync(new
        {
            // Línea 35-40: Campos obligatorios mapeados
            devEUI = alerta.DevEUI,
            lat = alerta.Lat,
            lon = alerta.Lon,
            bateria = alerta.Bateria,
            timestamp = utcTimestamp,
            fechaCreacion = utcFechaCreacion,
            nombre_victima = alerta.NombreVictima,
        });
    }

    // LÍNEA 59: IMPLEMENTACIÓN UpdateFieldsAsync
    public async Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates)
    {
        // Línea 61-62: Validación de alertaId
        if (string.IsNullOrEmpty(alertaId))
            throw new ArgumentException("alertaId es requerido", nameof(alertaId));

        // Línea 64: Obtener referencia del documento
        var docRef = _firestoreDb.Collection("alertas").Document(alertaId);
        
        // LÍNEA 66-79: Data Mapper - normalizar DateTime a Firestore.Timestamp
        var normalized = new Dictionary<string, object>();
        foreach (var kv in updates)
        {
            if (kv.Value is DateTime dt)
            {
                normalized[kv.Key] = Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
            }
            else if (kv.Value is DateTimeOffset dto)
            {
                normalized[kv.Key] = Google.Cloud.Firestore.Timestamp.FromDateTime(dto.UtcDateTime);
            }
            else
            {
                normalized[kv.Key] = kv.Value!;
            }
        }

        // LÍNEA 82: UPDATE documento en Firestore
        await docRef.UpdateAsync(normalized);
    }

    // LÍNEA 86: IMPLEMENTACIÓN ObtenerPorIdAsync
    public async Task<Alerta?> ObtenerPorIdAsync(string alertaId)
    {
        // Línea 88-89: Validación
        if (string.IsNullOrEmpty(alertaId))
            return null;

        // Línea 91-93: Obtener snapshot de Firestore
        var docRef = _firestoreDb.Collection("alertas").Document(alertaId);
        var snapshot = await docRef.GetSnapshotAsync();

        // Línea 95-97: Verificar existencia y extraer datos
        if (!snapshot.Exists)
            return null;
        var data = snapshot.ToDictionary();
    }
}
```

**Resumen Repository Pattern**:
- **Interfaz**: Líneas 7-29 de IAlertaRepository.cs
- **Implementación**: Líneas 10-150+ de AlertaRepositoryFirestore.cs
- **Métodos**: SaveAsync (L19), UpdateFieldsAsync (L59), ObtenerPorIdAsync (L86)

---

## 🎯 PATRÓN 2: DATA MAPPER PATTERN

**Qué es**: Mapeo bidireccional entre objetos dominio (Alerta) y persistencia (Firestore).

### Mapeo: Alerta → Firestore

**Ubicación**: AlertaRepositoryFirestore.cs, método `SaveAsync()`

```csharp
// LÍNEA 19-56: SaveAsync - MAPEO Alerta → Firestore (documento)
public async Task SaveAsync(Alerta alerta)
{
    // LÍNEA 21-25: Transformar Timestamp
    DateTime utcTimestamp = alerta.Timestamp.Kind == DateTimeKind.Utc
        ? alerta.Timestamp
        : alerta.Timestamp.ToUniversalTime();

    // LÍNEA 27-30: Transformar FechaCreacion
    DateTime utcFechaCreacion = alerta.FechaCreacion.Kind == DateTimeKind.Utc
        ? alerta.FechaCreacion
        : alerta.FechaCreacion.ToUniversalTime();

    // LÍNEA 32-56: Crear objeto anónimo con mapeos
    await _firestoreDb.Collection("alertas").AddAsync(new
    {
        // Mapeos campo por campo
        devEUI = alerta.DevEUI,                           // Línea 35
        lat = alerta.Lat,                                 // Línea 36
        lon = alerta.Lon,                                 // Línea 37
        bateria = alerta.Bateria,                         // Línea 38
        timestamp = utcTimestamp,                         // Línea 39 (UTC transformado)
        fechaCreacion = utcFechaCreacion,                // Línea 40 (UTC transformado)
        nombre_victima = alerta.NombreVictima,           // Línea 41 (snake_case)
        cantidadActivaciones = alerta.CantidadActivaciones,  // Línea 44
        ultimaActivacion = utcTimestamp,                 // Línea 45
        nivelUrgencia = alerta.NivelUrgencia,           // Línea 46
        esRecurrente = alerta.EsRecurrente,             // Línea 47
        estado = alerta.Estado,                           // Línea 50
    });
}
```

### Mapeo: Firestore → Alerta

**Ubicación**: AlertaRepositoryFirestore.cs, método `ObtenerPorIdAsync()`

```csharp
// LÍNEA 86-165: ObtenerPorIdAsync - MAPEO Firestore → Alerta (entidad)
public async Task<Alerta?> ObtenerPorIdAsync(string alertaId)
{
    // LÍNEA 91-93: Obtener documento
    var docRef = _firestoreDb.Collection("alertas").Document(alertaId);
    var snapshot = await docRef.GetSnapshotAsync();

    // LÍNEA 95-99: Extraer diccionario
    if (!snapshot.Exists) return null;
    var data = snapshot.ToDictionary();
    string documentId = snapshot.Id;

    // LÍNEA 101-102: Mapeo timestamp → DateTime
    DateTime fecha = DateTime.MinValue;
    if (data.ContainsKey("timestamp") && data["timestamp"] is Google.Cloud.Firestore.Timestamp ts)
    {
        fecha = ts.ToDateTime();  // Línea 105: Convertir Firestore.Timestamp → DateTime
    }

    // LÍNEA 107-108: Mapeo de valores numéricos
    double lat = data.ContainsKey("lat") ? Convert.ToDouble(data["lat"]) : 0;
    double lon = data.ContainsKey("lon") ? Convert.ToDouble(data["lon"]) : 0;
    double bateria = data.ContainsKey("bateria") ? Convert.ToDouble(data["bateria"]) : 0;

    // LÍNEA 110-111: Mapeo strings
    string deviceId = data.ContainsKey("device_id") ? data["device_id"]?.ToString() ?? "" : "";
    string nombreVictima = data.ContainsKey("nombre_victima") ? data["nombre_victima"]?.ToString() ?? "Sin asignar" : "Sin asignar";

    // LÍNEA 113: Mapeo estado
    string estado = data.ContainsKey("estado") ? data["estado"]?.ToString() ?? "disponible" : "disponible";

    // LÍNEAS 127-140+: Mapeos de fechas opcionales
    DateTime? fechaLlegada = null;
    if (data.ContainsKey("fechaLlegada") && data["fechaLlegada"] is Google.Cloud.Firestore.Timestamp tsLlegada)
    {
        fechaLlegada = tsLlegada.ToDateTime();
    }
    // ... más mapeos de fechas

    // LÍNEA 161+: crear objeto Alerta con datos mapeados
    return new Alerta(
        data["devEUI"]?.ToString() ?? "",
        lat,
        lon,
        bateria,
        fecha,
        fechaCreacion,
        deviceId,
        nombreVictima,
        estado,
        fechaLlegada,
        fechaAtendida,
        fechaResuelto,
        fechaTomada,
        patrulleroAsignado,
        cantidadActivaciones,
        ultimaActivacion,
        nivelUrgencia,
        esRecurrente
    );
}
```

**Mapeo DateTime especial**: Líneas 66-79 en `UpdateFieldsAsync()`

```csharp
// LÍNEA 66-79: Normalización de DateTime para UpdateAsync
var normalized = new Dictionary<string, object>();
foreach (var kv in updates)
{
    if (kv.Value is DateTime dt)
    {
        // LÍNEA 72: DateTime → Firestore.Timestamp (UTC)
        normalized[kv.Key] = Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
    }
    else if (kv.Value is DateTimeOffset dto)
    {
        // LÍNEA 76: DateTimeOffset → Firestore.Timestamp
        normalized[kv.Key] = Google.Cloud.Firestore.Timestamp.FromDateTime(dto.UtcDateTime);
    }
    else
    {
        normalized[kv.Key] = kv.Value!;
    }
}
```

**Resumen Data Mapper**:
- **Alerta → Firestore**: SaveAsync() líneas 19-56
- **Firestore → Alerta**: ObtenerPorIdAsync() líneas 86-165
- **DateTime Mapping**: UpdateFieldsAsync() líneas 66-79

---

## 🎯 PATRÓN 3: STRATEGY PATTERN (Semi-aplicado con condicionales)

**Qué es**: Diferentes estrategias algoritmo según contexto (alerta activa vs nueva vs recurrente).

### Ubicación: RegistrarAlertaUseCase.cs, método `EjecutarAsync()`

```csharp
// LÍNEA 12: Método principal
public async Task EjecutarAsync(Alerta nuevaAlerta)
{
    // LÍNEA 14-25: ESTRATEGIA 1 - Buscar alerta ACTIVA reciente
    var desde = DateTime.UtcNow.AddMinutes(-10);  // Línea 15: búsqueda últimos 10 min
    var alertaReciente = await _alertaRepository.BuscarAlertaRecienteAsync(
        nuevaAlerta.DevEUI,
        desde
    );

    // LÍNEA 27: FILTRAR alertas vencidas/no_resuelta
    if (alertaReciente != null && (alertaReciente.Estado == "vencida" || alertaReciente.Estado == "no_resuelta" || alertaReciente.Estado == "resuelto"))
    {
        alertaReciente = null;  // Línea 30: Ignorar alertas cerradas
    }

    // ===== RAMIFICACIÓN: SI EXISTE ALERTA ACTIVA =====
    if (alertaReciente != null)
    {
        // LÍNEA 32-46: ESTRATEGIA 1 - ACTUALIZAR ALERTA ACTIVA
        Console.WriteLine($"[UseCase] ✅ Alerta ACTIVA encontrada: id={alertaReciente.Id}");
        
        // Línea 35-36: Incrementar contador
        var nuevasCantidadActivaciones = alertaReciente.CantidadActivaciones + 1;
        var nuevoNivelUrgencia = CalcularNivelUrgencia(nuevasCantidadActivaciones);

        // Línea 38-45: Crear updates
        var updates = new Dictionary<string, object>
        {
            { "lat", nuevaAlerta.Lat },
            { "lon", nuevaAlerta.Lon },
            { "bateria", nuevaAlerta.Bateria },
            { "timestamp", DateTime.UtcNow },
            { "cantidadActivaciones", nuevasCantidadActivaciones },
            { "ultimaActivacion", DateTime.UtcNow },
            { "nivelUrgencia", nuevoNivelUrgencia }
        };
        
        // Línea 46: ACTUALIZAR (UPDATE)
        await _alertaRepository.UpdateFieldsAsync(alertaReciente.Id, updates);
    }
    // ===== RAMIFICACIÓN: SI NO EXISTE ALERTA ACTIVA =====
    else
    {
        // LÍNEA 51-115: ESTRATEGIA 2/3 - Buscar anterior Y decidir

        // LÍNEA 54-60: Buscar alerta ANTERIOR (cualquier estado)
        var todasLasAlertas = await _alertaRepository.ListarAlertasAsync();
        var alertaAnterior = todasLasAlertas
            .Where(a => a.DevEUI == nuevaAlerta.DevEUI)
            .OrderByDescending(a => a.UltimaActivacion)
            .FirstOrDefault();

        if (alertaAnterior != null)
        {
            // LÍNEA 66-75: Calcular tiempo transcurrido
            var tiempoTranscurrido = DateTime.UtcNow - alertaAnterior.UltimaActivacion;
            var horasTranscurridas = tiempoTranscurrido.TotalHours;

            // ===== ESTRATEGIA 2a: ALERTA NUEVA (anterior RESUELTA O >5h) =====
            // LÍNEA 79: Condicional principal
            if (alertaAnterior.Estado == "resuelto" || horasTranscurridas > 5)
            {
                // LÍNEA 87-89: Crear alerta NUEVA sin heredar
                nuevaAlerta.CantidadActivaciones = 1;
                nuevaAlerta.NivelUrgencia = "baja";
                nuevaAlerta.EsRecurrente = false;
            }
            // ===== ESTRATEGIA 2b: ALERTA RECURRENTE (<5h) =====
            else
            {
                // LÍNEA 98-115: Heredar datos y marcar recurrencia
                nuevaAlerta.CantidadActivaciones = alertaAnterior.CantidadActivaciones + 1;  // Línea 100
                nuevaAlerta.NivelUrgencia = "critica";  // Línea 101: Siempre crítica
                nuevaAlerta.EsRecurrente = true;        // Línea 102

                // LÍNEA 105-112: Marcar anterior como VENCIDA/NO_RESUELTA
                if (alertaAnterior.Estado == "disponible" || alertaAnterior.Estado == "tomada" || alertaAnterior.Estado == "llegada")
                {
                    string nuevoEstado = alertaAnterior.Estado == "tomada" ? "no_resuelta" : "vencida";
                    var updateEstado = new Dictionary<string, object> { { "estado", nuevoEstado } };
                    await _alertaRepository.UpdateFieldsAsync(alertaAnterior.Id, updateEstado);
                }
            }
        }

        // LÍNEA 120: GUARDAR la nueva alerta (en CUALQUIER estrategia)
        await _alertaRepository.SaveAsync(nuevaAlerta);
    }
}

// LÍNEA 142-148: MÉTODO HELPER (cálculo de urgencia)
private string CalcularNivelUrgencia(int cantidadActivaciones)
{
    if (cantidadActivaciones >= 4) return "critica";
    if (cantidadActivaciones >= 2) return "media";
    return "baja";
}
```

**Resumen Strategy Pattern**:
- **ESTRATEGIA 1**: Alerta activa (<10min) → actualizar (líneas 32-46)
- **ESTRATEGIA 2a**: Alerta anterior resuelta o >5h → nueva alerta (líneas 79-89)
- **ESTRATEGIA 2b**: Alerta anterior <5h → recurrencia (líneas 98-115)

---

## 🏗️ ARQUITECTURA: CLEAN ARCHITECTURE

### Capa 1: WebAPI (Presentación)

**Ubicación**: AlertaController.cs

```csharp
// LÍNEA 14: Controllador (Capa Presentación)
public class AlertaController : ControllerBase
{
    // LÍNEA 16-21: Inyecciones de dependencias (NO crea instancias internas)
    private readonly RegistrarAlertaUseCase _registrarAlertaUseCase;
    private readonly IUserRepositoryFirestore _userRepository;
    private readonly ListarAlertasUseCase _listarAlertasUseCase;
    private readonly IHubContext<AlertaHub> _hubContext;
    private readonly IAlertaRepository _alertaRepository;
    private readonly IFCMService _fcmService;

    // LÍNEA 23-32: Constructor (inyección)
    public AlertaController(
        RegistrarAlertaUseCase registrarAlertaUseCase,
        ListarAlertasUseCase listarAlertasUseCase,
        IUserRepositoryFirestore userRepository,
        IHubContext<AlertaHub> hubContext,
        IAlertaRepository alertaRepository,
        IFCMService fcmService)
    {
        // Asignación de dependencias
    }

    // LÍNEA 37: HTTP Endpoint (Presentación)
    [HttpPost("lorawan-webhook")]
    public async Task<IActionResult> RegistrarLorawanWebhook([FromBody] JsonElement data)
    {
        // Solo orquestación, NO lógica de negocio
    }
}
```

**Capas separadas**:
```
LÍNEA 1-37 (AlertaController)    ← WebAPI Layer (Presentación)
                    ↓
LÍNEA 12 (EjecutarAsync)         ← Application Layer (Use Case)
            ↓
LÍNEA 14 (BuscarAlertaRecienteAsync)  ← Domain Layer (Interfaz)
            ↓
LÍNEA 19-82 (AlertaRepositoryFirestore) ← Infrastructure (Implementación)
            ↓
Firestore DB ← Base de Datos
```

---

## 🏗️ ARQUITECTURA: USE CASE PATTERN

**Ubicación**: RegistrarAlertaUseCase.cs

```csharp
// LÍNEA 4: Clase Use Case (una operación = una clase)
public class RegistrarAlertaUseCase
{
    // LÍNEA 6: Inyección de dependencia (interfaz, no implementación)
    private readonly IAlertaRepository _alertaRepository;

    // LÍNEA 8: Constructor
    public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
    {
        _alertaRepository = alertaRepository;
    }

    // LÍNEA 12: Método Ejecutar (patrón Use Case)
    public async Task EjecutarAsync(Alerta nuevaAlerta)
    {
        // Lógica completa de registrar alerta
        // (estrategias, cálculos, pasos)
    }
}
```

**Otros use cases**:
- `ListarAlertasUseCase` - listar alertas
- `ActualizarUbicacionPatrullaUseCase` - actualizar ubicación
- etc.

---

## ⚙️ PRINCIPIO: DEPENDENCY INJECTION

### En AlertaController

```csharp
// LÍNEA 16-21: Campos privados (no crea instancias)
private readonly RegistrarAlertaUseCase _registrarAlertaUseCase;
private readonly IUserRepositoryFirestore _userRepository;
private readonly ListarAlertasUseCase _listarAlertasUseCase;
private readonly IHubContext<AlertaHub> _hubContext;
private readonly IAlertaRepository _alertaRepository;
private readonly IFCMService _fcmService;

// LÍNEA 23-32: Constructor - las dependencias VIENEN de afuera
public AlertaController(
    RegistrarAlertaUseCase registrarAlertaUseCase,    // ← Inyectada
    ListarAlertasUseCase listarAlertasUseCase,        // ← Inyectada
    IUserRepositoryFirestore userRepository,           // ← Interfaz inyectada
    IHubContext<AlertaHub> hubContext,                // ← Interfaz inyectada
    IAlertaRepository alertaRepository,               // ← Interfaz inyectada
    IFCMService fcmService)                           // ← Interfaz inyectada
{
    _registrarAlertaUseCase = registrarAlertaUseCase;
    _listarAlertasUseCase = listarAlertasUseCase;
    _userRepository = userRepository;
    _hubContext = hubContext;
    _alertaRepository = alertaRepository;
    _fcmService = fcmService;
}

// LÍNEA 169: Uso de dependencia (NO crea con 'new')
await _registrarAlertaUseCase.EjecutarAsync(alerta);
```

### En RegistrarAlertaUseCase

```csharp
// LÍNEA 6: Campo privado (depende de abstracción, no implementación)
private readonly IAlertaRepository _alertaRepository;

// LÍNEA 8: Constructor - inyección
public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
{
    _alertaRepository = alertaRepository;
}

// LÍNEA 22: Uso (podría ser AlertaRepositoryFirestore, AlertaRepositorySQL, AlertaRepositoryMongo)
var alertaReciente = await _alertaRepository.BuscarAlertaRecienteAsync(
    nuevaAlerta.DevEUI,
    desde
);
```

---

## 🧠 ENFOQUE: DOMAIN-DRIVEN DESIGN

### Rich Domain Model: Alerta.cs

```csharp
// LÍNEA 12: Clase Entidad de Dominio
public class Alerta
{
    // Línea 28-31: Propiedades de negocio
    public int CantidadActivaciones { get; set; } = 1;
    public DateTime UltimaActivacion { get; set; }
    public string NivelUrgencia { get; set; } = "baja";
    public bool EsRecurrente { get; set; } = false;

    // LÍNEA 93-97: MÉTODO DE DOMINIO (lógica de negocio VIVE en la entidad)
    public string CalcularNivelUrgencia()
    {
        if (CantidadActivaciones >= 4) return "critica";
        if (CantidadActivaciones >= 2) return "media";
        return "baja";
    }

    // LÍNEA 99-104: MÉTODO DE DOMINIO (verificación de archivación)
    public bool DebeArchivarse()
    {
        var tiempoLimite = FechaCreacion.AddHours(5);
        return DateTime.UtcNow > tiempoLimite && Estado == "disponible";
    }

    // LÍNEA 106-111: MÉTODO DE DOMINIO (verificación de rango temporal)
    public bool EstaDentroDelRango()
    {
        var tiempoLimite = UltimaActivacion.AddMinutes(10);
        return DateTime.UtcNow <= tiempoLimite;
    }

    // LÍNEA 113-127: MÉTODO DE DOMINIO (incrementar activación con actualización automática)
    public void IncrementarActivacion()
    {
        CantidadActivaciones++;
        UltimaActivacion = DateTime.UtcNow;
        Timestamp = DateTime.UtcNow;
        NivelUrgencia = CalcularNivelUrgencia();  // Actualización automática
        if (CantidadActivaciones > 1)
        {
            EsRecurrente = true;
        }
    }

    // LÍNEA 129-133: MÉTODO DE DOMINIO (marcar como vencida)
    public void MarcarComoVencida()
    {
        Estado = "vencida";
    }
}
```

**Por qué es DDD**:
- ✅ Métodos viven en la entidad (NO en servicios)
- ✅ Reglas de negocio encapsuladas (CalcularNivelUrgencia, DebeArchivarse, etc.)
- ✅ La entidad es "inteligente", no solo contenedor de datos
- ✅ Lógica interdependiente encapsulada (IncrementarActivacion actualiza múltiples fields)

---

## 💫 BUENA PRÁCTICA: ENUMS

### Ubicación: Alerta.cs, líneas 1-10

```csharp
// LÍNEA 1: Enum para ESTADOS SEGUROS (tipo-seguro)
public enum AlertaEstado
{
    Disponible,     // Línea 3
    Tomada,         // Línea 4
    Llegada,        // Línea 5
    Atendida,       // Línea 6
    Resuelto,       // Línea 7
    NoAtendida,     // Línea 8
    Vencida,        // Línea 9 ← Property que usa esto
    NoResuelta      // Línea 10
}

// LÍNEA 135+: Propedad que usa enum (conversión segura)
public AlertaEstado EstadoAlerta
{
    get
    {
        return Estado.ToLower() switch
        {
            "disponible" => AlertaEstado.Disponible,
            "tomada" => AlertaEstado.Tomada,
            "llegada" => AlertaEstado.Llegada,
            // ... etc
        };
    }
}
```

---

## 📊 TABLA RESUMEN: LÍNEAS EXACTAS POR ARCHIVO

| Archivo | Patrón/Principio | Líneas | Tipo |
|---------|---|---|---|
| **IAlertaRepository.cs** | Interface (Repository) | 7-29 | Contrato |
| **AlertaRepositoryFirestore.cs** | Implementación Repository | 10-150+ | Implementación |
| **AlertaRepositoryFirestore.cs** | Data Mapper (SaveAsync) | 19-56 | Mapeo |
| **AlertaRepositoryFirestore.cs** | Data Mapper (ObtenerPorId) | 86-165 | Mapeo |
| **AlertaRepositoryFirestore.cs** | DateTime Normalización | 66-79 | Helper |
| **RegistrarAlertaUseCase.cs** | Use Case Pattern | 4-148 | Arquitectura |
| **RegistrarAlertaUseCase.cs** | Strategy Pattern | 27-115 | Lógica algorítmica |
| **RegistrarAlertaUseCase.cs** | Dependency Injection | 6-10 | Principio |
| **AlertaController.cs** | Dependency Injection | 16-32 | Principio |
| **AlertaController.cs** | Clean Architecture | 14-250+ | Arquitectura |
| **Alerta.cs** | Enum Type Safety | 1-10 | Buena práctica |
| **Alerta.cs** | Rich Domain Model | 93-133 | Enfoque DDD |
| **Alerta.cs** | Domain Methods | 93-148 | Encapsulación |
| **UserRepositoryFirestore.cs** | Repository (BuscarPorDeviceId) | 81-88 | Patrón |
| **UserRepositoryFirestore.cs** | Dependency Injection | 13-16 | Principio |

---

## 🎯 CÓMO NAVEGARLO

**Si quieres ver un patrón específico**:

1. Ve a la sección del patrón
2. Encuentra el archivo
3. Busca las LÍNEAS exactas
4. Lee el código con números de línea

**Si quieres entender un archivo completo**:

Busca el archivo en la tabla → encontrarás todas sus líneas con patrones

**Si quieres comparar dos patrones**:

Tienen secciones separadas → compara lado a lado

