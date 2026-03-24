# 🏆 ANÁLISIS DE BUENAS PRÁCTICAS EN LOS 6 ARCHIVOS DEL MÓDULO 1

---

## 📚 ÍNDICE DE CONTENIDOS

1. **Patrones de Diseño Puros (GoF)**
2. **Patrones Arquitectónicos**
3. **Principios e Inyección de Dependencias**
4. **Enfoques de Diseño (DDD)**
5. **Buenas Prácticas Técnicas**
6. **Buenas Prácticas por Archivo**
7. **Buenas Prácticas en Relaciones entre Archivos**
8. **Matriz de Patrones SOLID**
9. **Análisis de Código Limpio**
10. **Recomendaciones de Mejora**

---

## 🎨 1. PATRONES DE DISEÑO PUROS (GoF - Gang of Four)

### ✅ PATRÓN 1: REPOSITORY PATTERN

**Definición**: Encapsula la lógica de acceso a datos detrás de una abstracción, desacoplando dominio de persistencia.

**Familia**: Patrón estructural / Creacional

**Implementación**:

#### IAlertaRepository.cs (Interfaz):
```csharp
public interface IAlertaRepository
{
    Task SaveAsync(Alerta alerta);
    Task<List<Alerta>> ListarAlertasAsync();
    Task<Alerta?> ObtenerPorIdAsync(string alertaId);
    Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates);
    Task<Alerta?> BuscarAlertaRecienteAsync(string deviceId, DateTime desde);
}
```

#### AlertaRepositoryFirestore.cs (Implementación):
```csharp
public class AlertaRepositoryFirestore : IAlertaRepository
{
    private readonly FirestoreDb _firestoreDb;

    public async Task SaveAsync(Alerta alerta)
    {
        await _firestoreDb.Collection("alertas").AddAsync(new { ... });
    }

    public async Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates)
    {
        var docRef = _firestoreDb.Collection("alertas").Document(alertaId);
        var normalized = new Dictionary<string, object>();
        foreach (var kv in updates)
        {
            if (kv.Value is DateTime dt)
            {
                normalized[kv.Key] = Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
            }
        }
    }
}
```

**Por qué es patrón puro**:
- ✅ Mapea entre dominio (Alerta) y persistencia (Firestore)
- ✅ Interfaz clara: contrato explícito
- ✅ Implementación intercambiable (podrías cambiar a MongoDB sin tocar dominio)
- ✅ Mantiene lógica de BD centralizada

---

### ✅ PATRÓN 2: DATA MAPPER PATTERN

**Definición**: Separa mapping entre objetos de dominio y representación en BD, evitando que la entidad conozca la BD.

**Familia**: Patrón estructural

**Implementación en AlertaRepositoryFirestore**:

```csharp
// De Alerta (dominio) → Firestore (BD)
await _firestoreDb.Collection("alertas").AddAsync(new
{
    devEUI = alerta.DevEUI,
    lat = alerta.Lat,
    lon = alerta.Lon,
    bateria = alerta.Bateria,
    timestamp = utcTimestamp,
    nombre_victima = alerta.NombreVictima,              // ← Mapping
    cantidadActivaciones = alerta.CantidadActivaciones,
    nivelUrgencia = alerta.NivelUrgencia,
    esRecurrente = alerta.EsRecurrente,
});

// De Firestore → Alerta (dominio)
var alerta = new Alerta(
    data["devEUI"]?.ToString() ?? "",
    lat, lon, bateria,
    // ... mapeo inverso
);
```

**Por qué es patrón puro**:
- ✅ Bidireccional: Alerta ↔ Firestore
- ✅ Normalización (DateTime → Firestore.Timestamp)
- ✅ Entidad no conoce estructura Firestore
- ✅ Cambios en BD = cambios solo en mapper

---

### ✅ PATRÓN 3: STRATEGY PATTERN

**Definición**: Define familia de algoritmos, encapsula cada uno, los hace intercambiables.

**Familia**: Patrón de comportamiento

**Implementación en RegistrarAlertaUseCase.EjecutarAsync()**:

```csharp
// ESTRATEGIA 1: Alerta ACTIVA reciente (<10min) → ACTUALIZAR
if (alertaReciente != null)
{
    var nuevoNivel = CalcularNivelUrgencia(cantidadActivaciones);
    await _alertaRepository.UpdateFieldsAsync(alertaReciente.Id, updates);
}
// ESTRATEGIA 2a: Sin alerta activa + anterior RESUELTA/VIEJA → NUEVA ALERTA
else if (alertaAnterior?.Estado == "resuelto" || horasTranscurridas > 5)
{
    nuevaAlerta.CantidadActivaciones = 1;
    nuevaAlerta.NivelUrgencia = "baja";
    nuevaAlerta.EsRecurrente = false;
}
// ESTRATEGIA 2b: Sin alerta activa + anterior RECIENTE → RECURRENCIA
else
{
    nuevaAlerta.CantidadActivaciones = alertaAnterior.CantidadActivaciones + 1;
    nuevaAlerta.NivelUrgencia = "critica";
    nuevaAlerta.EsRecurrente = true;
}
```

**Por qué es patrón puro**:
- ✅ Tres estrategias intercambiables según contexto
- ✅ Lógica clara, fácil de testear
- ✅ Cada rama es independiente conceptualmente

**⚠️ NOTA**: Strategy aquí es **semi-aplicado** (condicionales en lugar de clases Strategy). Para puro Strategy, sería:

```csharp
// Puro Strategy (si lo fuera):
interface IAlertaStrategy { Task Ejecutar(Alerta alerta); }
class AlertaActivaStrategy : IAlertaStrategy { /* actualizar */ }
class AlertaNuevaStrategy : IAlertaStrategy { /* crear nueva */ }
class AlertaRecurrenteStrategy : IAlertaStrategy { /* marcar recurrencia */ }
```

Aquí usamos **algorithmic branching** en lugar de Strategy puro.

---

## 🏗️ 2. PATRONES ARQUITECTÓNICOS

### ✅ PATRÓN A: CLEAN ARCHITECTURE (Separación en Capas)

**Definición**: Organiza el código en capas concéntricas independientes, minimizando dependencias hacia adentro.

**Implementación en nuestro código**:

```
WebAPI Layer (Presentación)
  ↓ AlertaController.cs
Application Layer (Lógica de Negocio)
  ↓ RegistrarAlertaUseCase.cs
Domain Layer (Lógica de Dominio)
  ↓ Alerta.cs + IAlertaRepository.cs (interfaz)
Infrastructure Layer (Datos)
  ↓ AlertaRepositoryFirestore.cs + UserRepositoryFirestore.cs
```

**Archivos**: TODOS los 6

**Evidencia en código**:
- ✅ AlertaController.cs: Recibe HTTP, NO accede a BD directamente
- ✅ RegistrarAlertaUseCase.cs: Lógica pura, NO depende de HTTP
- ✅ Alerta.cs: Entidad de dominio, NO sabe de Firestore
- ✅ AlertaRepositoryFirestore.cs: Única clase que conoce Firestore

---

### ✅ PATRÓN B: USE CASE PATTERN (Aplicación)

**Definición**: Cada operación de negocio es una clase aislada (similar a Command Pattern pero a nivel arquitectura).

**Implementación**:

```csharp
public class RegistrarAlertaUseCase
{
    private readonly IAlertaRepository _alertaRepository;

    public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
    {
        _alertaRepository = alertaRepository;
    }

    public async Task EjecutarAsync(Alerta nuevaAlerta)
    {
        // Lógica de negocio: calcular urgencia, detectar recurrencia
    }
}
```

**Beneficio**:
- ✅ **Single Responsibility**: 1 clase = 1 caso de uso
- ✅ **Testeable**: Puedes pasar mock de repository
- ✅ **Reutilizable**: Controller lo usa, tests lo usan
- ✅ **Escalable**: Fácil agregar nuevos use cases

---

## ⚙️ 3. PRINCIPIOS E INYECCIÓN DE DEPENDENCIAS

### ✅ PRINCIPIO: DEPENDENCY INJECTION (DI)

## ⚙️ 3. PRINCIPIOS E INYECCIÓN DE DEPENDENCIAS

### ✅ PRINCIPIO: DEPENDENCY INJECTION (DI)

**Definición**: Las dependencias se inyectan en lugar de crearse internamente (Inversion of Control).

**Implementación**:

#### En AlertaController (línea 23-40):
```csharp
public AlertaController(
    RegistrarAlertaUseCase registrarAlertaUseCase,      // Inyectada
    ListarAlertasUseCase listarAlertasUseCase,          // Inyectada
    IUserRepositoryFirestore userRepository,             // Interfaz inyectada
    IHubContext<AlertaHub> hubContext,                   // Interfaz inyectada
    IAlertaRepository alertaRepository,                  // Interfaz inyectada
    IFCMService fcmService)                              // Interfaz inyectada
{
    _registrarAlertaUseCase = registrarAlertaUseCase;    // Asignada
    _userRepository = userRepository;
}
```

#### En RegistrarAlertaUseCase (línea 6-10):
```csharp
public class RegistrarAlertaUseCase
{
    private readonly IAlertaRepository _alertaRepository;

    public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
    {
        _alertaRepository = alertaRepository;
    }
}
```

**Beneficio**:
- ✅ NO hace `new RegistrarAlertaUseCase()` → Usa inyección
- ✅ NO hace `new UserRepositoryFirestore()` → Depende de interfaz
- ✅ Todas las dependencias llegan por constructor
- ✅ Constructor es `readonly` → No puede reasignarse
- ✅ Facilita testing (puedes pasar mocks)

---

## 🧠 4. ENFOQUES DE DISEÑO

### ✅ ENFOQUE: DOMAIN-DRIVEN DESIGN (DDD)

**Definición**: Entidades de dominio con lógica de negocio (rich models), no solo contenedores de datos.

**Implementación en Alerta.cs**:

```csharp
public class Alerta
{
    public int CantidadActivaciones { get; set; } = 1;
    public DateTime UltimaActivacion { get; set; }
    public string NivelUrgencia { get; set; } = "baja";
    public bool EsRecurrente { get; set; } = false;

    // ✅ MÉTODOS de dominio (RICH MODEL)
    
    public string CalcularNivelUrgencia()
    {
        if (CantidadActivaciones >= 4) return "critica";
        if (CantidadActivaciones >= 2) return "media";
        return "baja";
    }

    public bool DebeArchivarse()
    {
        var tiempoLimite = FechaCreacion.AddHours(5);
        return DateTime.UtcNow > tiempoLimite && Estado == "disponible";
    }

    public bool EstaDentroDelRango()
    {
        var tiempoLimite = UltimaActivacion.AddMinutes(10);
        return DateTime.UtcNow <= tiempoLimite;
    }

    public void IncrementarActivacion()
    {
        CantidadActivaciones++;
        UltimaActivacion = DateTime.UtcNow;
        Timestamp = DateTime.UtcNow;
        NivelUrgencia = CalcularNivelUrgencia();
        if (CantidadActivaciones > 1)
        {
            EsRecurrente = true;
        }
    }
}
```

**Beneficio**:
- ✅ Entidad NO es solo contenedor (anemic model)
- ✅ Tiene reglas de negocio encapsuladas
- ✅ Mejora testabilidad: `alerta.DebeArchivarse()` vs lógica suelta
- ✅ Self-documenting: el código explica las reglas

---

## ⚠️ 5. BUENAS PRÁCTICAS TÉCNICAS

### ✅ PRÁCTICA 1: ENUMS PARA TYPE-SAFETY

**Definición**: Usar enumeraciones en lugar de strings mágicos para valores predefinidos.

**Implementación en Alerta.cs**:

```csharp
public enum AlertaEstado
{
    Disponible,     // 0
    Tomada,         // 1
    Llegada,        // 2
    Atendida,       // 3
    Resuelto,       // 4
    NoAtendida,     // 5
    Vencida,        // 6
    NoResuelta      // 7
}

public AlertaEstado EstadoAlerta
{
    get
    {
        return Estado.ToLower() switch
        {
            "disponible" => AlertaEstado.Disponible,
            "tomada" => AlertaEstado.Tomada,
            // ...
        };
    }
}
```

**Beneficio**:
- ✅ Type-safety: No puedes pasar estado inválido
- ✅ IDE autocomplete: AlertaEstado.Tomada (no "tomada" string)
- ✅ Reduce bugs: typos son errores de compilación

---

### ✅ PRÁCTICA 2: ASYNC/AWAIT PATTERN

**Definición**: Operaciones asincrónicas con async/await, evitando bloqueos.

**Implementación**:

```csharp
// AlertaController
public async Task<IActionResult> RegistrarLorawanWebhook()
{
    await _userRepository.BuscarPorDeviceIdAsync(deviceId);
    await _registrarAlertaUseCase.EjecutarAsync(alerta);
    return Ok();
}

// RegistrarAlertaUseCase
public async Task EjecutarAsync(Alerta nuevaAlerta)
{
    var alertaReciente = await _repository.BuscarAlertaRecienteAsync(...);
    await _repository.SaveAsync(nuevaAlerta);
}
```

**Beneficio**:
- ✅ No bloquea thread por I/O
- ✅ Escalable: mismo thread sirve múltiples requests
- ✅ Respuestas rápidas incluso con BD lenta

---

### ✅ PRÁCTICA 3: NULL COALESCING & GUARDS

**Definición**: Validación explícita con null coalescing y guard clauses.

**Implementación en AlertaController**:

```csharp
// Null coalescing
var deviceId = body["end_device_ids"]["device_id"]?.ToString() ?? string.Empty;

// Guard clause
if (string.IsNullOrEmpty(deviceId))
{
    return BadRequest("deviceId es obligatorio");
}
```

**Beneficio**:
- ✅ Evita null reference exceptions
- ✅ Código más legible
- ✅ Flujo claro: qué debe pasar vs qué son excepciones

---

## 📊 RESUMEN: CLASIFICACIÓN CORRECTA

| Elemento | Tipo | Archivo | Estado |
|----------|------|---------|--------|
| **Repository** | ✅ Patrón GoF | IAlertaRepository, AlertaRepositoryFirestore | Bien aplicado |
| **Data Mapper** | ✅ Patrón GoF | AlertaRepositoryFirestore | Bien aplicado |
| **Strategy** | ⚠️ Patrón GoF | RegistrarAlertaUseCase | Semi-aplicado (branching, no clases) |
| **Clean Arch** | 🏗️ Arquitectura | Todos | Bien aplicado |
| **Use Case** | 🏗️ Arquitectura | RegistrarAlertaUseCase | Bien aplicado |
| **DI** | ⚙️ Principio | AlertaController, UseCase | Bien aplicado |
| **DDD** | 🧠 Enfoque | Alerta.cs | Bien aplicado |
| **Enum** | 💫 Buena práctica | Alerta.cs | Bien aplicado |
| **Async/Await** | 💫 Buena práctica | Todos | Bien aplicado |
| **Null Safety** | 💫 Buena práctica | AlertaController | Bien aplicado |

---

---

## 🔍 2. BUENAS PRÁCTICAS POR ARCHIVO

---

### 📄 ARCHIVO 1: AlertaController.cs

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Inyección de Dependencias** | 23-40 | Constructor recibe todas las dependencias |
| 2 | **Async/Await completo** | 43, 145, 181, 213 | Todos los métodos son `async Task<>` |
| 3 | **Separación de responsabilidades** | 39-230 | Controller solo orquesta, no contiene lógica |
| 4 | **Try-Catch para decodificación** | 73-79 | Maneja `FormatException` de Base64 |
| 5 | **Fallback strategy** | 113-128 | GPS fallback si `frm_payload` falla → `locations.user` |
| 6 | **Null coalescing** | 167 | `deviceId ?? string.Empty` → evita null reference |
| 7 | **Guard clauses** | 143 | Valida datos ANTES de procesarlos |
| 8 | **Logging explícito** | 45, 225 | Console.WriteLine para depuración |
| 9 | **Exception handling gracioso** | 215 | No interrumpe flujo si FCM falla |
| 10 | **DTO separado** | 167 | Crea nueva instancia Alerta con datos validados |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **Console.WriteLine en producción** | 45 | Usar `ILogger<AlertaController>` |
| 2 | **Exception genérica** | 137 | Capturar `JsonException` + `FormatException` específicamente |
| 3 | **Magic strings** | 68, 102, 115 | Definir constantes: `const string PAYLOAD_KEY = "frm_payload"` |
| 4 | **Sin validación de formato GPS** | 99 | Validar que lat ∈ [-90, 90], lon ∈ [-180, 180] |

---

### 📄 ARCHIVO 2: RegistrarAlertaUseCase.cs

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Single Responsibility** | 4-150 | Una clase = una operación (Registrar Alerta) |
| 2 | **Algoritmo claro documentado** | 17-145 | Comentarios explícitos de cada rama |
| 3 | **Timescale-based decisioning** | 29 | Busca alertas últimos 10 min (configurable) |
| 4 | **State filtering** | 26-29 | Ignora alertas vencidas/resueltas |
| 5 | **Recurrence detection** | 70-98 | Lógica: <5hrs = recurrencia, >5hrs = nueva |
| 6 | **Transactional updates** | 39-47 | Actualiza múltiples campos atómicamente |
| 7 | **Async chain** | 22, 38, 50, 109 | Todas las operaciones BD son async |
| 8 | **Logging detallado** | Varias líneas | Logs muestran decisiones tomadas |
| 9 | **Helper method** | 143-148 | `CalcularNivelUrgencia()` desacoplado |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **Magic numbers** | 29, 74, 91 | `-10 minutos`, `5 horas` → constantes configurables |
| 2 | **ListarAlertasAsync completo** | 61 | No escalable: carga TODAS las alertas. Usar query Firestore: `WHERE devEUI = X AND timestamp >= Y` |
| 3 | **Console.WriteLine** | Múltiples | `ILogger` injection |
| 4 | **Hardcoded strings** | 26, 60 | Estados como enums: `Estados.Disponible` |

---

### 📄 ARCHIVO 3: Alerta.cs (Domain Entity)

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Enum para estados** | 1-8 | `AlertaEstado` enum (type-safe) |
| 2 | **Múltiples constructores** | 43-52, 59-88 | Sobrecarga: crear desde webhook VS BD |
| 3 | **Default values** | 22-28 | Propiedades con defaults: `= 1`, `= "baja"` |
| 4 | **Rich domain model** | 102-138 | Métodos: CalcularNivelUrgencia, DebeArchivarse, etc. |
| 5 | **Immutability (readonly)** | - | Propiedades auto properties (no setters privados) |
| 6 | **DateTime handling** | 11 | Timestamps in separate fields: Timestamp vs FechaCreacion |
| 7 | **Nullable types** | 32-37 | `DateTime?` para fechas opcionales |
| 8 | **Property grouping** | 11-37 | Agrupa por tipo: obligatorias, urgencia, opcionales |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **String estado** | 35 | Cambiar a `AlertaEstado` enum |
| 2 | **Manual UTC conversion** | Implicado | Crear método `NormalizarTimestamps()` |
| 3 | **Constructores complejos** | 43-88 | Usar builder pattern si sigue creciendo |
| 4 | **Sin validación** | - | ValidarDatos() en constructor |

---

### 📄 ARCHIVO 4: AlertaRepositoryFirestore.cs

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Implementa interfaz** | 11 | `: IAlertaRepository` → contrato explícito |
| 2 | **UTC normalization** | 20-30 | Convierte DateTime a UTC siempre |
| 3 | **DateTime ↔ Firestore.Timestamp** | 70-80 | Mapeo bidireccional automático |
| 4 | **Null safety en extracción** | 137-180 | Verifica `ContainsKey()` antes de leer |
| 5 | **Collection name centralized** | 35 | `Collection("alertas")` = constante única |
| 6 | **Async throughout** | Métodos | Todos retornan `Task<>` |
| 7 | **Fluent queries** | 195-198 | `.OrderByDescending("timestamp")` |
| 8 | **Error handling** | 64-66 | Valida IDs antes de eliminar |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **Magic string "alertas"** | 35, 198 | `private const string COLLECTION_NAME = "alertas"` |
| 2 | **Duplicate Timestamp logic** | 70-80 | Crear método helper `NormalizeDateTime()` reutilizable |
| 3 | **Duplicate MapearDesdeFirestore** | Implícito | Compartir con otros repositorios (DRY) |
| 4 | **No logging** | - | Agregar `ILogger<>` para queries lentas |
| 5 | **ListarAlertasAsync sin paginación** | 198 | `.Limit(1000)` para grandes datasets |

---

### 📄 ARCHIVO 5: UserRepositoryFirestore.cs

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Implementa interfaz** | 11 | `: IUserRepositoryFirestore` |
| 2 | **Query method for correlation** | 80-87 | `BuscarPorDeviceIdAsync()` → encapsula lógica |
| 3 | **Async operations** | Todos | Queries ejecutadas vía `GetSnapshotAsync()` |
| 4 | **Conversión automática** | 21, 86 | `.ConvertTo<UsuarioDto>()` mapping automático |
| 5 | **Multiple query methods** | 19-87 | Por DNI, UID, rol, deviceId → flexible |
| 6 | **Null-safe returns** | 86 | Retorna `null` si no encontrado (client maneja) |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **Excessive Console.WriteLine** | 19, 25, 26, 27 | Usar `ILogger`, no debug logs en producción |
| 2 | **No caching** | - | Usuario frecuentemente buscado → Redis cache |
| 3 | **Magic string "users"** | 20, etc. | `const string COLLECTION_NAME = "users"` |
| 4 | **Sin índices Firestore** | - | Indexar: `device_id`, `dni`, `role` |
| 5 | **Foreach genérico** | 21-25 | Usar `.FirstOrDefaultAsync()` si esperas 1 resultado |

---

### 📄 ARCHIVO 6: IAlertaRepository.cs (Interfaz)

#### 🟢 BUENAS PRÁCTICAS IMPLEMENTADAS

| # | Buena Práctica | Líneas | Evidencia |
|---|---|---|---|
| 1 | **Abstract interface** | 5-20 | Define contrato, no implementación |
| 2 | **Clear method names** | Todo | `SaveAsync`, `UpdateFieldsAsync` = naming convención |
| 3 | **Async methods** | Todo | Todos retornan `Task<>` o `Task` |
| 4 | **Nullable returns** | 10, 17 | `Alerta?` → manejo explícito de no encontrado |
| 5 | **Flexible updates** | 11 | `IDictionary<string, object>` → genérico |
| 6 | **Future-proof** | 19-24 | Métodos definidos para funcionalidades futuras |

#### 🟡 OPORTUNIDADES DE MEJORA

| # | Problema | Línea | Solución |
|---|---|---|---|
| 1 | **Métodos sin doc XML** | - | `/// <summary>` para IDE intellisense |
| 2 | **Sin validación de parámetros** | - | Documentar pre-condiciones (ej: "alertaId != null") |

---

## 🔗 3. BUENAS PRÁCTICAS EN RELACIONES ENTRE ARCHIVOS

### 🔄 RELACIÓN 1: Controller → UseCase (Inyección de UseCase)

```csharp
// AlertaController.cs (línea 16)
private readonly RegistrarAlertaUseCase _registrarAlertaUseCase;

// Constructor (línea 24)
public AlertaController(..., RegistrarAlertaUseCase registrarAlertaUseCase, ...)
{
    _registrarAlertaUseCase = registrarAlertaUseCase;  // Inyectada
}

// Uso (línea 168)
await _registrarAlertaUseCase.EjecutarAsync(alerta);
```

**Buena Práctica**:
- ✅ **Loose coupling**: Controller no crea UseCase
- ✅ **Testeable**: Puedes pasar mock de UseCase
- ✅ **Configuración centralizada**: DI container configura en Program.cs

---

### 🔄 RELACIÓN 2: UseCase → Repository (Interfaz)

```csharp
// RegistrarAlertaUseCase.cs (línea 6-7)
private readonly IAlertaRepository _alertaRepository;

// Constructor (línea 9)
public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
{
    _alertaRepository = alertaRepository;
}

// Uso (línea 22, 38, 50)
await _alertaRepository.BuscarAlertaRecienteAsync(nuevaAlerta.DevEUI, desde);
await _alertaRepository.UpdateFieldsAsync(alertaReciente.Id, updates);
await _alertaRepository.SaveAsync(nuevaAlerta);
```

**Buena Práctica**:
- ✅ **Depende de interfaz, NO de implementación**:
  - ❌ MALO: `new AlertaRepositoryFirestore()`
  - ✅ BUENO: Recibe `IAlertaRepository`
- ✅ **Intercambiable**: Si mañana usas MongoDB, cambias solo la implementación
- ✅ **Testeable**: Tests usan mock que implementa `IAlertaRepository`

---

### 🔄 RELACIÓN 3: Repository → Entity (Data Mapping)

```csharp
// AlertaRepositoryFirestore.SaveAsync() (línea 35-56)
await _firestoreDb.Collection("alertas").AddAsync(new
{
    devEUI = alerta.DevEUI,              // Entity field → Firestore field
    lat = alerta.Lat,
    nombre_victima = alerta.NombreVictima,  // ⚠️ MAPEO: NombreVictima → nombre_victima
    cantidadActivaciones = alerta.CantidadActivaciones,
    nivelUrgencia = alerta.NivelUrgencia,
    // ...
});
```

**Buena Práctica**:
- ✅ **Entidad desconoce Firestore**: Alerta.cs NO importa Google.Cloud.Firestore
- ✅ **Mapeo centralizado**: Un solo lugar que conoce campos Firestore
- ✅ **Naming conventions**: Entity usa PascalCase, Firestore usa snake_case

---

### 🔄 RELACIÓN 4: Controller → Repository (Para correlación)

```csharp
// AlertaController.cs (línea 17-18)
private readonly IUserRepositoryFirestore _userRepository;

// Uso (línea 145)
victima = await _userRepository.BuscarPorDeviceIdAsync(deviceId);

// UserRepositoryFirestore.cs (línea 80-87)
public async Task<UsuarioDto?> BuscarPorDeviceIdAsync(string deviceId)
{
    var query = _firestoreDb.Collection("users").WhereEqualTo("device_id", deviceId);
    var snapshot = await query.GetSnapshotAsync();
    foreach (var doc in snapshot.Documents)
    {
        return doc.ConvertTo<UsuarioDto>();
    }
    return null;
}
```

**Buena Práctica**:
- ✅ **Separación clara**: Controller NO consulta directamente Firestore
- ✅ **Encapsulación de query Firestore**: Toda lógica BD en repositorio
- ✅ **Retorna DTO**: Controller NO recibe entidades de BD, sino DTOs

---

## 📊 4. MATRIZ DE PRINCIPIOS SOLID

| Principio | Implementado | Archivo | Evidencia |
|-----------|---|---|---|
| **S** (Single Responsibility) | ✅ SÍ | RegistrarAlertaUseCase | Una clase = un algoritmo de urgencia |
| **O** (Open/Closed) | ✅ SÍ | IAlertaRepository | Abierto para mongo, db2; cerrado para modificación |
| **L** (Liskov Substitution) | ✅ SÍ | AlertaRepositoryFirestore | Puede reemplazarse por cualquier impl. `IAlertaRepository` |
| **I** (Interface Segregation) | ✅ SÍ | IAlertaRepository, IUserRepositoryFirestore | Interfaces pequeñas, específicas |
| **D** (Dependency Inversion) | ✅ SÍ | AlertaController | Depende de abstracciones (interfaces), NO de concretas |

---

## 💫 5. ANÁLISIS DE CÓDIGO LIMPIO

### 🟢 CLEAN CODE PRESENT

| Práctica | Archivos | Ejemplo |
|----------|----------|---------|
| **Nombres descriptivos** | Todos | `RegistrarAlertaUseCase`, `BuscarAlertaRecienteAsync`, `CantidadActivaciones` |
| **Funciones pequeñas** | RegistrarAlertaUseCase | `CalcularNivelUrgencia()` = 5 líneas |
| **Sin side effects visuales** | Todos | Métodos hacen 1 cosa |
| **Manejo de errores explícito** | AlertaController | Try-catch con tipo específico: `FormatException` |
| **Comments explícitos** | RegistrarAlertaUseCase | `// 🔥 INCREMENTAR CONTADOR...` documentan decisiones |
| **Async/await consistente** | Todos | No hay `.Result` bloqueante |

### 🟡 CLEAN CODE OPPORTUNITIES

| Problema | Localización | Mejora |
|----------|---|---|
| **Magic numbers** | RegistrarAlertaUseCase L29 | `-10` minutos → constante `DELAY_RECIENTE` |
| **Magic strings** | AlertaController | "frm_payload", "GPS" → `const` |
| **Console.WriteLine** | Múltiples archivos | Cambiar a `ILogger<>` inyectado |
| **Métodos muy largos** | RegistrarAlertaUseCase | L17-145: 130 líneas, podría dividirse |
| **Comentarios obvios** | Varios | "INCREMENTAR CONTADOR" es obvio de `++` |

---

## ⚠️ 6. PROBLEMAS IDENTIFICADOS

### 🔴 PROBLEMAS CRÍTICOS

| Problema | Ubicación | Severidad | Impacto |
|----------|-----------|----------|---------|
| **Console.WriteLine en producción** | AlertaController, RegistrarAlertaUseCase, UserRepositoryFirestore | 🔴 ALTA | DNIs, alertaIds expuestos en logs públicos |
| **Validación GPS incompleta** | AlertaController L99 | 🟠 MEDIA | Acepta lat=91 (inválido) |
| **Sin paginación** | AlertaRepositoryFirestore L198 | 🟠 MEDIA | Si 10k+ alertas → OutOfMemory |
| **Query sin índice Firestore** | UserRepositoryFirestore L80 | 🟠 MEDIA | `device_id` query = lenta sin índice |

### 🟡 PROBLEMAS MAYORES

| Problema | Ubicación | Mejora |
|----------|-----------|--------|
| **Magic numbers/strings** | Múltiples | Crear InvalidAlertaSettings.cs o constantes |
| **Duplicate timestamp logic** | AlertaRepositoryFirestore | Crear DateTime mapper helper |
| **Listando todas las alertas** | RegistrarAlertaUseCase L61 | Query en BD: `WHERE devEUI=X AND timestamp>Y` |
| **No hay builder pattern** | Alerta.cs | Si constructores crecen, usar builder |

---

## 🚀 7. RECOMENDACIONES POR PRIORIDAD

### 🔴 CRÍTICAS (Implementar ahora)

1. **Reemplazar Console.WriteLine con ILogger**
   ```csharp
   // ❌ ANTES
   Console.WriteLine($"[UseCase] ✅ Alerta ACTIVA encontrada");

   // ✅ DESPUÉS
   _logger.LogInformation("Alerta activa encontrada: {AlertaId}", alertaReciente.Id);
   ```

2. **Crear constantes para magic numbers/strings**
   ```csharp
   public static class AlertaConstants
   {
       public const int MINUTOS_RECIENTE = 10;
       public const int HORAS_RESET_CONTADOR = 5;
       public const int ACTIVACIONES_MEDIA = 2;
       public const int ACTIVACIONES_CRITICA = 4;
   }
   ```

3. **Validar ranges GPS**
   ```csharp
   if (!(lat >= -90 && lat <= 90) || !(lon >= -180 && lon <= 180))
       return BadRequest("Coordenadas GPS inválidas");
   ```

### 🟠 IMPORTANTES (Próxima sprint)

4. **Query en BD en lugar de ListarAlertasAsync()**
   ```csharp
   public async Task<List<Alerta>> BuscarAlertasPorDeviceIdAsync(string devEUI, DateTime desde)
   {
       var query = _firestoreDb.Collection("alertas")
           .WhereEqualTo("devEUI", devEUI)
           .WhereGreaterThanOrEqualTo("timestamp", desde)
           .OrderByDescending("timestamp")
           .Limit(10);  // ← PAGINACIÓN
       return await query.GetSnapshotAsync();
   }
   ```

5. **Agregar índices Firestore**
   - Collection: `users`, Field: `device_id`
   - Collection: `alertas`, Field: `devEUI, timestamp`

6. **Crear DateTime mapper helper**
   ```csharp
   public static class FirestoreTimestampHelper
   {
       public static object NormalizeDate(object value)
       {
           if (value is DateTime dt)
               return Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
           return value;
       }
   }
   ```

### 🟡 MENORES (Backlog)

7. **Documentar con XML docs**
   ```csharp
   /// <summary>
   /// Busca alertas activas en los últimos 10 minutos.
   /// </summary>
   /// <param name="deviceId">Identificador del dispositivo</param>
   /// <param name="desde">Rango de tiempo</param>
   /// <returns>Alerta encontrada o null</returns>
   public async Task<Alerta?> BuscarAlertaRecienteAsync(string deviceId, DateTime desde)
   ```

8. **Refactorizar RegistrarAlertaUseCase en métodos privados**
   ```csharp
   private async Task ActualizarAlertaActiva(Alerta alertaReciente, Alerta nuevaAlerta)
   private async Task CrearNuevaAlerta(Alerta nuevaAlerta, Alerta alertaAnterior)
   private async Task ProcesarRecurrencia(Alerta nuevaAlerta, Alerta alertaAnterior)
   ```

---

## 📋 RESUMEN: SCORECARD DE CALIDAD

| Aspecto | Calificación | Observación |
|---------|---|---|
| **Separación de capas** | A+ | Clean Architecture clara |
| **Inyección de dependencias** | A | Bien implementado, sin hardcoding |
| **Repository pattern** | A- | Bien, pero sin paginación |
| **Manejo de errores** | B+ | Try-catch Present, pero sin logging estructurado |
| **Async/Await** | A | Consistente en todos lados |
| **Nombre y legibilidad** | A | Descriptivos y claros |
| **Testing** | C | No hay tests unitarios manuales evidentes |
| **Logging** | D | Console.WriteLine NO es production-ready |
| **Magic numbers** | C- | Muchos sin documentación |
| **Documentación** | C | Pocas XML docs |
| **OVERALL** | B+ | Sistema bien arquitectado, necesita pulido |

---

## 🎯 CONCLUSIÓN

Los 6 archivos del Módulo 1 demuestran **buena comprensión de arquitectura limpia y patrones de diseño**:

✅ **Fortalezas**:
- Separación clara en capas
- DI correctamente implementado
- Repository pattern bien encapsulado
- Async/await consistente
- Buena naming

❌ **Debilidades**:
- Logging con Console.WriteLine
- Magic numbers/strings sin constantes
- Sin paginación en queries largos
- Tests unitarios no evidentes

🚀 **Siguiente paso**: Aplicar recomendaciones críticas (logging, constantes, validaciones) en los 6 archivos.
