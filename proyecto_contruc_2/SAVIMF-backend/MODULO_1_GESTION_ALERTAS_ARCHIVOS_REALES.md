# 📋 MÓDULO 1: GESTIÓN DE ALERTAS
## Identificación Exacta de Archivos y Código (Sin Mentiras)

**Requisitos Funcionales Cubiertos**: RF-008, RF-009, RF-024
- ✅ Recibir alertas desde TTS
- ✅ Decodificar datos
- ✅ Correlacionar dispositivo con víctima
- ✅ Calcular nivel de urgencia

---

## 🎯 ARCHIVOS QUE COMPONEN EL MÓDULO 1

### 1️⃣ **AlertaController.cs**
**Ruta**: `backend_alert_csharp/backend_alert/WebAPI/Controllers/AlertaController.cs`  
**Responsabilidad**: RECEPTOR DEL WEBHOOK + ORQUESTADOR

#### ✅ Recibir alertas desde TTS
```csharp
// LÍNEA 39: Endpoint que recibe el webhook
[HttpPost("lorawan-webhook")]
public async Task<IActionResult> RegistrarLorawanWebhook([FromBody] JsonElement data)
```
- **Qué hace**: Recibe JSON POST desde The Things Stack
- **Cómo**: IActionResult obtiene JsonElement con toda la estructura TTS

#### ✅ Decodificar datos
```csharp
// LÍNEAS 50-147: Parse del JSON de TTS
string? devEUI = null;          // Línea 51
string? deviceId = null;        // Línea 52
double? lat = null;             // Línea 53
double? lon = null;             // Línea 54
double? bateria = null;         // Línea 55
DateTime timestamp = DateTime.UtcNow;  // Línea 56

// LÍNEA 59: Extrae DevEUI desde end_device_ids
if (data.TryGetProperty("end_device_ids", out JsonElement endDeviceIds))
{
    if (endDeviceIds.TryGetProperty("dev_eui", out JsonElement devEuiProp))
        devEUI = devEuiProp.GetString();  // LÍNEA 63

    if (endDeviceIds.TryGetProperty("device_id", out JsonElement deviceIdProp))
        deviceId = deviceIdProp.GetString();  // LÍNEA 66
}

// LÍNEA 68-74: DECODIFICACIÓN DE Base64 (frm_payload)
if (data.TryGetProperty("uplink_message", out JsonElement uplinkMessage) &&
    uplinkMessage.TryGetProperty("frm_payload", out JsonElement frmPayloadProp))
{
    var frmPayloadBase64 = frmPayloadProp.GetString();  // LÍNEA 68
    if (!string.IsNullOrEmpty(frmPayloadBase64))
    {
        byte[] decodedBytes;
        try
        {
            decodedBytes = Convert.FromBase64String(frmPayloadBase64);  // LÍNEA 74
        }
        catch (FormatException)
        {
            return BadRequest(new { mensaje = "Formato de frm_payload inválido" });
        }

        // LÍNEA 79: Convierte bytes a string UTF-8
        string payloadDecoded = System.Text.Encoding.UTF8.GetString(decodedBytes);

        // LÍNEA 81: Parsea el JSON descomprimido
        using var doc = JsonDocument.Parse(payloadDecoded);
        var payloadJson = doc.RootElement;

        // LÍNEA 84: Extrae GPS desde JSON descodificado
        if (payloadJson.TryGetProperty("GPS", out JsonElement gpsProp))
        {
            var gpsString = gpsProp.GetString();  // "lat,lon"
            if (!string.IsNullOrEmpty(gpsString))
            {
                var coords = gpsString.Split(',');
                if (coords.Length == 2 &&
                    double.TryParse(coords[0], out var latVal) &&
                    double.TryParse(coords[1], out var lonVal))
                {
                    lat = latVal;
                    lon = lonVal;
                }
            }
        }

        // LÍNEA 103: Extrae Battery
        if (payloadJson.TryGetProperty("Battery", out JsonElement batteryProp) &&
            batteryProp.TryGetDouble(out var batteryVal))
        {
            bateria = batteryVal;
        }
    }
}

// LÍNEA 115-128: Fallback GPS desde locations.user (si frm_payload falló)
if ((lat == null || lon == null) &&
    data.TryGetProperty("uplink_message", out JsonElement uplinkMessage2))
{
    // Extrae coordenadas del campo locations.user de TTS
}

// LÍNEA 130: Extrae timestamp de received_at
if (data.TryGetProperty("received_at", out JsonElement receivedAtProp) &&
    DateTime.TryParse(receivedAtProp.GetString(), out var ts))
{
    timestamp = ts;
}
```

**DECODIFICACIÓN EXACTA**:
1. JSON POST → `end_device_ids.device_id` y `end_device_ids.dev_eui`
2. `uplink_message.frm_payload` (Base64) → `Convert.FromBase64String()` → bytes
3. Bytes → `UTF-8.GetString()` → JSON string
4. JSON string → `JsonDocument.Parse()` → object
5. Extract `GPS`, `Battery` fields
6. Split GPS: `"lat,lon"` → `double[] {lat, lon}`

#### ✅ Correlacionar dispositivo con víctima
```csharp
// LÍNEA 143: Busca víctima por deviceId
UsuarioDto? victima = null;
string nombreVictima = "Sin asignar";
if (!string.IsNullOrEmpty(deviceId))
{
    victima = await _userRepository.BuscarPorDeviceIdAsync(deviceId);  // LÍNEA 145
    if (victima != null)
    {
        nombreVictima = victima.Nombre;
        apellidoVictima = victima.Apellido;
        dniVictima = victima.Dni;
    }
}
```

**FLUJO DE CORRELACIÓN**:
1. Obtiene `deviceId` del webhook TTS
2. Llama a `_userRepository.BuscarPorDeviceIdAsync(deviceId)` (repositorio)
3. Si encuentra usuario en Firestore:
   - Extrae Nombre, Apellido, DNI
   - Asigna `nombreVictima = victima.Nombre`
4. Si NO encuentra:
   - Deja `nombreVictima = "Sin asignar"`

#### 📤 Crea Alerta + Orquesta flujo
```csharp
// LÍNEA 167: Crea ENTIDAD Alerta con datos decodificados
var alerta = new Alerta(
    devEUI,
    lat.Value,
    lon.Value,
    bateria.Value,
    timestamp,
    deviceId ?? string.Empty,
    $"{nombreVictima} {apellidoVictima}"  // Nombre correlacionado
);

// LÍNEA 168: DELEGA lógica de urgencia a UseCase
await _registrarAlertaUseCase.EjecutarAsync(alerta);

// LÍNEA 181: SignalR broadcast a web dashboard
await _hubContext.Clients.All.SendAsync("RecibirAlerta", new
{
    estado = "Despachada",
    nombre = nombreVictima,
    lat = lat,
    lon = lon,
    // ...
});

// LÍNEA 195-210: FCM push a patrulleros
var tokensFcm = await _userRepository.ObtenerTokensFcmPorRoleAsync("patrullero", soloActivos: true);
if (tokensFcm.Any())
{
    await _fcmService.EnviarNotificacionMultipleAsync(tokensFcm, titulo, contenido, alertaData);
}
```

---

### 2️⃣ **RegistrarAlertaUseCase.cs**
**Ruta**: `backend_alert_csharp/backend_alert/Application/UseCases/RegistrarAlertaUseCase.cs`  
**Responsabilidad**: LÓGICA DE URGENCIA + DETECCIÓN DE RECURRENCIA

#### ✅ Calcular nivel de urgencia

```csharp
// LÍNEA 12-18: Constructor e inyección de dependencias
public class RegistrarAlertaUseCase
{
    private readonly IAlertaRepository _alertaRepository;

    public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
    {
        _alertaRepository = alertaRepository;
    }

// LÍNEA 20: MÉTODO PRINCIPAL: EjecutarAsync
public async Task EjecutarAsync(Alerta nuevaAlerta)
{
    // LÍNEA 22: Busca alertas ACTIVAS del mismo dispositivo en últimos 10 minutos
    var desde = DateTime.UtcNow.AddMinutes(-10);
    var alertaReciente = await _alertaRepository.BuscarAlertaRecienteAsync(
        nuevaAlerta.DevEUI,
        desde
    );

    // LÍNEA 28-33: Filtra solo alertas ACTIVAS (ignora vencidas/resueltas)
    if (alertaReciente != null && (alertaReciente.Estado == "vencida" || 
                                   alertaReciente.Estado == "no_resuelta" || 
                                   alertaReciente.Estado == "resuelto"))
    {
        alertaReciente = null;  // Ignora si NO está activa
    }

    // CASO 1: Hay alerta ACTIVA en últimos 10 minutos
    if (alertaReciente != null)
    {
        // LÍNEA 36: INCREMENTA contador de activaciones
        var nuevasCantidadActivaciones = alertaReciente.CantidadActivaciones + 1;
        
        // LÍNEA 37: CALCULA nivel de urgencia basado en activaciones
        var nuevoNivelUrgencia = CalcularNivelUrgencia(nuevasCantidadActivaciones);

        // LÍNEA 39-45: Actualiza los campos en Firestore
        var updates = new Dictionary<string, object>
        {
            { "lat", nuevaAlerta.Lat },                           // GPS actualizado
            { "lon", nuevaAlerta.Lon },                           // GPS actualizado
            { "bateria", nuevaAlerta.Bateria },                   // Battery actualizada
            { "timestamp", DateTime.UtcNow },                     // Timestamp actual
            { "cantidadActivaciones", nuevasCantidadActivaciones },  // CRÍTICO: Incrementa contador
            { "ultimaActivacion", DateTime.UtcNow },              // Marca última activación
            { "nivelUrgencia", nuevoNivelUrgencia }               // CRUCIAL: Actualiza urgencia
        };
        await _alertaRepository.UpdateFieldsAsync(alertaReciente.Id, updates);
    }
    
    // CASO 2: NO hay alerta activa reciente
    else
    {
        // LÍNEA 57: Busca la alerta más reciente del dispositivo (cualquier estado)
        var todasLasAlertas = await _alertaRepository.ListarAlertasAsync();
        var alertaAnterior = todasLasAlertas
            .Where(a => a.DevEUI == nuevaAlerta.DevEUI)
            .OrderByDescending(a => a.UltimaActivacion)
            .FirstOrDefault();

        if (alertaAnterior != null)
        {
            // LÍNEA 70: Calcula tiempo transcurrido desde última activación
            var tiempoTranscurrido = DateTime.UtcNow - alertaAnterior.UltimaActivacion;
            var horasTranscurridas = tiempoTranscurrido.TotalHours;

            // LÍNEA 74-88: LÓGICA DE DECISIÓN
            // Si la alerta anterior ESTÁ RESUELTA O pasaron >5 horas:
            //   → Crear alerta completamente NUEVA (reset cantidadActivaciones)
            if (alertaAnterior.Estado == "resuelto" || horasTranscurridas > 5)
            {
                nuevaAlerta.CantidadActivaciones = 1;           // Reset
                nuevaAlerta.NivelUrgencia = "baja";             // Reinicia en bajo
                nuevaAlerta.EsRecurrente = false;               // NO es recurrencia
            }
            else
            {
                // LÍNEA 90-98: Si pasó MENOS de 5 horas:
                //   → Es RECURRENCIA: heredar datos + marcar CRÍTICA
                nuevaAlerta.CantidadActivaciones = alertaAnterior.CantidadActivaciones + 1;
                nuevaAlerta.NivelUrgencia = "critica";          // SIEMPRE crítica en recurrencia
                nuevaAlerta.EsRecurrente = true;                // MARCA como recurrente

                // LÍNEA 101: Marca la alerta anterior como "no_resuelta" o "vencida"
                string nuevoEstado = alertaAnterior.Estado == "tomada" ? "no_resuelta" : "vencida";
                await _alertaRepository.UpdateFieldsAsync(alertaAnterior.Id, 
                    new Dictionary<string, object> { { "estado", nuevoEstado } });
            }
        }

        // LÍNEA 109: Guarda la NUEVA alerta en Firestore
        await _alertaRepository.SaveAsync(nuevaAlerta);
    }
}

// LÍNEA 113-120: MÉTODO DE CÁLCULO DE URGENCIA
private string CalcularNivelUrgencia(int cantidadActivaciones)
{
    if (cantidadActivaciones >= 4) return "critica";       // 4+ activaciones = CRÍTICA
    if (cantidadActivaciones >= 2) return "media";         // 2-3 activaciones = MEDIA
    return "baja";                                          // 1 activación = BAJA
}
```

**ALGORITMO EXACTO DE URGENCIA**:
```
SI CantidadActivaciones >= 4:
    NivelUrgencia = "critica" ⚠️ ROJO
SINO SI CantidadActivaciones >= 2:
    NivelUrgencia = "media" 🟡 AMARILLO
SINO:
    NivelUrgencia = "baja" 🟢 VERDE
```

**ALGORITMO EXACTO DE RECURRENCIA**:
```
SI existe_alerta_anterior_hace_<10min:
    SI estado_anterior == "ACTIVA":
        → ACTUALIZAR alerta anterior (incrementar activaciones, cambiar urgencia)
    SINO:
        → EVALUAR tiempo desde última activación
        SI horasTranscurridas > 5 O estado_anterior == "resuelto":
            → CREAR NUEVA ALERTA (CantidadActivaciones = 1, reset)
        SINO:
            → CREAR ALERTA RECURRENTE (heredar contador, marcar critica)
```

---

### 3️⃣ **Alerta.cs** (Domain Entity)
**Ruta**: `backend_alert_csharp/backend_alert/Domain/Entities/Alerta.cs`  
**Responsabilidad**: MODELO DE DOMINIO CON CAMPOS DE URGENCIA

#### Campos de Urgencia
```csharp
// LÍNEA 18: Contador de activaciones
public int CantidadActivaciones { get; set; } = 1;

// LÍNEA 19: timestamp de última activación
public DateTime UltimaActivacion { get; set; }

// LÍNEA 20: Nivel de urgencia calculado
public string NivelUrgencia { get; set; } = "baja"; // "baja", "media", "critica"

// LÍNEA 21: Bandera si es alerta recurrente
public bool EsRecurrente { get; set; } = false;
```

#### Constructor principal (desde webhook)
```csharp
// LÍNEA 42-51: Constructor para alertas nuevas
public Alerta(string devEUI, double lat, double lon, double bateria, 
              DateTime timestamp, string deviceId, string nombreVictima)
{
    DevEUI = devEUI;
    Lat = lat;
    Lon = lon;
    Bateria = bateria;
    Timestamp = timestamp;
    FechaCreacion = timestamp;  // Se asigna UNA SOLA VEZ
    DeviceId = deviceId;
    NombreVictima = nombreVictima;

    // LÍNEA 51-54: Inicializa campos de urgencia
    CantidadActivaciones = 1;
    UltimaActivacion = timestamp;
    NivelUrgencia = "baja";
    EsRecurrente = false;

    Estado = "disponible";
    // ... otros campos
}

// LÍNEA 59-88: Constructor para alertas leídas de Firestore
public Alerta(string devEUI, double lat, double lon, double bateria, 
              DateTime timestamp, DateTime fechaCreacion,
              string deviceId, string nombreVictima, string estado, 
              DateTime? fechaLlegada, DateTime? fechaAtendida,
              DateTime? fechaResuelto, DateTime? fechaTomada, 
              string patrulleroAsignado,
              int cantidadActivaciones = 1, DateTime? ultimaActivacion = null, 
              string nivelUrgencia = "baja", bool esRecurrente = false)
{
    // ... asigna todos los campos, incluyendo los de urgencia
    CantidadActivaciones = cantidadActivaciones;
    UltimaActivacion = ultimaActivacion ?? timestamp;
    NivelUrgencia = nivelUrgencia;
    EsRecurrente = esRecurrente;
}
```

---

### 4️⃣ **AlertaRepositoryFirestore.cs** (Infrastructure/Persistence)
**Ruta**: `backend_alert_csharp/backend_alert/Infrastructure/Persistence/AlertaRepositoryFirestore.cs`  
**Responsabilidad**: PERSISTENCIA EN FIRESTORE

#### Guardando alertas con campos de urgencia
```csharp
// LÍNEA 18-56: SaveAsync - Guarda nueva alerta en Firestore
public async Task SaveAsync(Alerta alerta)
{
    DateTime utcTimestamp = alerta.Timestamp.Kind == DateTimeKind.Utc
        ? alerta.Timestamp
        : alerta.Timestamp.ToUniversalTime();

    DateTime utcFechaCreacion = alerta.FechaCreacion.Kind == DateTimeKind.Utc
        ? alerta.FechaCreacion
        : alerta.FechaCreacion.ToUniversalTime();

    // LÍNEA 35: Guardando en colección "alertas"
    await _firestoreDb.Collection("alertas").AddAsync(new
    {
        devEUI = alerta.DevEUI,
        lat = alerta.Lat,
        lon = alerta.Lon,
        bateria = alerta.Bateria,
        timestamp = utcTimestamp,
        fechaCreacion = utcFechaCreacion,
        nombre_victima = alerta.NombreVictima,

        // LÍNEA 43-46: GUARDANDO CAMPOS DE URGENCIA
        cantidadActivaciones = alerta.CantidadActivaciones,
        ultimaActivacion = utcTimestamp,
        nivelUrgencia = alerta.NivelUrgencia,              // 🔴 GUARDADO CRÍTICO
        esRecurrente = alerta.EsRecurrente,

        estado = alerta.Estado,
        fechaLlegada = alerta.FechaLlegada,
        fechaAtendida = alerta.FechaAtendida,
        fechaResuelto = alerta.FechaResuelto,
        fechaTomada = alerta.FechaTomada,
        patrulleroAsignado = alerta.PatrulleroAsignado
    });
}

// LÍNEA 58: UpdateFieldsAsync - Actualiza campos específicos
public async Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates)
```

**Ejemplo de documento guardado en Firestore "alertas" collection**:
```json
{
  "devEUI": "70B3D57ED003F00F",
  "lat": -12.0463,
  "lon": -76.9971,
  "bateria": 98.5,
  "timestamp": "2025-03-22T15:30:45.123Z",
  "fechaCreacion": "2025-03-22T15:30:45.123Z",
  "nombre_victima": "Maria Rodriguez",
  "cantidadActivaciones": 3,
  "ultimaActivacion": "2025-03-22T15:32:15.000Z",
  "nivelUrgencia": "media",
  "esRecurrente": false,
  "estado": "tomada",
  "fechaTomada": "2025-03-22T15:31:00.000Z"
}
```

---

### 5️⃣ **UserRepositoryFirestore.cs** (Infrastructure/Persistence)
**Ruta**: `backend_alert_csharp/backend_alert/Infrastructure/Persistence/UserRepositoryFirestore.cs`  
**Responsabilidad**: BUSCAR USUARIO POR DEVICEID PARA CORRELACIÓN

```csharp
// LÍNEA 80-87: BuscarPorDeviceIdAsync - Correlaciona dispositivo con víctima
public async Task<UsuarioDto?> BuscarPorDeviceIdAsync(string deviceId)
{
    // Ejecuta query Firestore: WHERE device_id = deviceId
    var query = _firestoreDb.Collection("users").WhereEqualTo("device_id", deviceId);
    var snapshot = await query.GetSnapshotAsync();

    // Retorna el primer resultado encontrado
    foreach (var doc in snapshot.Documents)
    {
        return doc.ConvertTo<UsuarioDto>();  // Convierte documento a objeto Usuario
    }
    return null;  // No encontrado
}
```

**Ejemplo de documento encontrado en Firestore "users" collection**:
```json
{
  "uid": "firebase_uid_xyz",
  "email": "maria@example.com",
  "dni": "12345678",
  "nombre": "Maria",
  "apellido": "Rodriguez",
  "role": "victima",
  "device_id": "paxel-vic-01",
  "estado": "activo"
}
```

---

### 6️⃣ **ValidadorDatosService.cs** (Infrastructure/Services)
**Ruta**: `backend_alert_csharp/backend_alert/Infrastructure/Services/ValidadorDatosService.cs`  
**Responsabilidad**: VALIDAR DATOS DECODIFICADOS

```csharp
// LÍNEA 20-27: ValidarDatosAlerta - Valida campos básicos de alerta
public bool ValidarDatosAlerta(Alerta alerta)
{
    return !string.IsNullOrEmpty(alerta.DevEUI)    // DevEUI no vacío
        && alerta.Lat != 0                          // Latitud válida (no cero)
        && alerta.Lon != 0                          // Longitud válida (no cero)
        && alerta.Bateria != 0;                     // Batería válida (no cero)
}
```

**Qué valida**:
- ✅ DevEUI presente
- ✅ Coordenadas GPS válidas (lat/lon != 0)
- ✅ Valor de batería presente

---

## 🔄 FLUJO COMPLETO: PASO A PASO

```
1️⃣ RECEPCIÓN (AlertaController - líneas 39-147)
   TTS webhook POST /api/alerta/lorawan-webhook
   │
   ├─ Extrae DevEUI, deviceId de end_device_ids
   ├─ Decodifica Base64 frm_payload → JSON
   ├─ Extrae GPS, Battery del JSON decodificado
   └─ Obtiene timestamp de received_at

2️⃣ CORRELACIÓN (AlertaController - líneas 143-155)
   deviceId → BuscarPorDeviceIdAsync()
   │
   └─ Firestore collection "users" WHERE device_id = deviceId
      → Obtiene: Nombre, Apellido, DNI de la víctima

3️⃣ VALIDACIÓN (AlertaController + ValidadorDatosService)
   ✅ Valida DevEUI, Lat, Lon, Bateria no vacíos/cero

4️⃣ CREACIÓN DE ENTIDAD (AlertaController - línea 167)
   NEW Alerta(
       devEUI,
       lat, lon, bateria,
       timestamp,
       deviceId,
       nombreVictima_correlacionado
   )
   │
   └─ Inicializa:
      - CantidadActivaciones = 1
      - NivelUrgencia = "baja"
      - EsRecurrente = false
      - Estado = "disponible"

5️⃣ CÁLCULO DE URGENCIA (RegistrarAlertaUseCase - líneas 20-120)
   BuscarAlertaRecienteAsync(devEUI, últimos_10_min)
   │
   ├─ SI existe alerta ACTIVA:
   │  └─ CantidadActivaciones++ → CalcularNivelUrgencia()
   │     └─ SI >= 4: "critica" | SI >= 2: "media" | SINO: "baja"
   │
   └─ SI NO existe:
      └─ Buscar alerta anterior (cualquier estado)
         ├─ SI resuelta O pasaron >5 horas:
         │  └─ NUEVA alerta (reset contador)
         └─ SI pasó <5 horas:
            └─ RECURRENCIA (incrementar contador, marcar crítica, EsRecurrente=true)

6️⃣ PERSISTENCIA (AlertaRepositoryFirestore - líneas 18-56)
   Firestore.Collection("alertas").AddAsync({
       devEUI, lat, lon, bateria,
       "cantidadActivaciones": nuevasActivaciones,
       "nivelUrgencia": nivelCalculado,
       "esRecurrente": marcado,
       ... otros campos
   })

7️⃣ DIFUSIÓN EN TIEMPO REAL (AlertaController - líneas 181-210)
   ├─ SignalR: Broadcast a web dashboard
   └─ FCM: Push a todos los patrulleros con tokens activos
```

---

## 📊 RESUMEN: ARCHIVOS REALES DEL MÓDULO 1

| # | Archivo | Ruta | Líneas | Función Exacta |
|---|---------|------|--------|---|
| 1 | **AlertaController.cs** | `WebAPI/Controllers/` | 39-147 | Recibe webhook TTS, decodifica Base64, extrae GPS/Battery, busca víctima |
| 2 | **RegistrarAlertaUseCase.cs** | `Application/UseCases/` | 20-120 | Calcula NivelUrgencia, detecta recurrencia, determina si es alerta nueva o actualización |
| 3 | **Alerta.cs** | `Domain/Entities/` | 1-100 | Define campos: CantidadActivaciones, NivelUrgencia, EsRecurrente, UltimaActivacion |
| 4 | **AlertaRepositoryFirestore.cs** | `Infrastructure/Persistence/` | 18-56 | Guarda alerta en Firestore collection "alertas" con campos de urgencia |
| 5 | **UserRepositoryFirestore.cs** | `Infrastructure/Persistence/` | 80-87 | BuscarPorDeviceIdAsync: correlaciona device_id con usuario/víctima en Firestore |
| 6 | **ValidadorDatosService.cs** | `Infrastructure/Services/` | 20-27 | Valida DevEUI, Lat, Lon, Bateria no vacíos/cero |

---

## 🎖️ CAMPOS DE URGENCIA GUARDADOS EN FIRESTORE

| Campo | Tipo | Valores | Significado |
|-------|------|--------|---|
| `cantidadActivaciones` | int | 1, 2, 3, 4+ | Número de reactivaciones en corto tiempo |
| `nivelUrgencia` | string | "baja", "media", "critica" | Prioridad calculada |
| `esRecurrente` | bool | true/false | ¿Es reactivación del mismo dispositivo <5h? |
| `ultimaActivacion` | DateTime | ISO 8601 | Timestamp de última reactivación |

---

## ✅ REQUISITOS MAPEADOS

| RF | Descripción | Archivo | Líneas |
|----|-------------|---------|--------|
| RF-008 | Recibir alertas desde TTS | AlertaController.cs | 39-57 |
| RF-009 | Decodificar datos LoRaWAN (Base64) | AlertaController.cs | 68-103 |
| RF-024 | Calcular nivel de urgencia | RegistrarAlertaUseCase.cs | 113-120 |
| (Implícito) | Correlacionar dispositivo con víctima | AlertaController.cs + UserRepositoryFirestore.cs | 143-155 + 80-87 |
| (Implícito) | Detectar recurrencia | RegistrarAlertaUseCase.cs | 70-98 |

---

## 🚀 PUNTO DE ENTRADA FINAL

El flujo completo inicia en:
```
POST /api/alerta/lorawan-webhook
→ AlertaController.RegistrarLorawanWebhook()
→ RegistrarAlertaUseCase.EjecutarAsync()
→ AlertaRepositoryFirestore.SaveAsync() O UpdateFieldsAsync()
→ Firestore collection "alertas"
```

**Colección Firestore destino**: `alertas`  
**Documentos generados**: 1 nuevo documento por alerta (o actualización si es reactivación dentro de 10 min)

