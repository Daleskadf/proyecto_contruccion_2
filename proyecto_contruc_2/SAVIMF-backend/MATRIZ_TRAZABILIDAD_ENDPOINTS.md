# 📊 MATRIZ DE TRAZABILIDAD: Endpoints → Use Cases → Repositories → Firestore

## ARQUITECTURA DE DATOS

```
HTTP Endpoint (WebAPI) 
    ↓
Controlador → Use Case (Application Layer)
    ↓
Repository Interface (Abstraction)
    ↓
Repository Implementation (Firestore)
    ↓
Firestore Collection + Operaciones CRUD
```

---

## 1️⃣ CONTROLLER: AlertaController (`/api/alerta/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 1 | `/lorawan-webhook` | POST | RegistrarAlertaUseCase | IAlertaRepository | `alertas` | ❌ Pública | ✅ SignalR broadcast a web<br>✅ FCM multicast a patrulleros<br>✅ Busca víctima en `users` |
| 2 | `/` | GET | ListarAlertasUseCase | IAlertaRepository | `alertas` | 🔐 Firebase Auth | — |
| 3 | `/rango` | GET | — (directo del repo) | IAlertaRepository | `alertas` | 🔐 Firebase Auth | Filtra por FechaCreacion |
| 4 | `/activas` | GET | ListarAlertasUseCase | IAlertaRepository | `alertas` | 🔐 Firebase Auth | Marca vencidas primero |
| 5 | `/tomar` | POST | — (directo) | IAlertaRepository | `alertas` | 🔐 Firebase Auth | Actualiza: Estado→"tomada", PatrulleroAsignado |
| 6 | `/cambiar-estado` | POST | — (directo) | IAlertaRepository | `alertas` | 🔐 Firebase Auth | Actualiza Estado + timestamp según nuevo estado |

---

## 2️⃣ CONTROLLER: PatrullaController (`/api/patrulla/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 7 | `/ubicacion` | POST | ActualizarUbicacionPatrullaUseCase | IPatrulleroRepository | `ubicaciones_patrullas` | 🔐 Firebase Auth | ✅ Haversine: Si ≤30m de alerta.Lat/Lon<br>→ Auto-transición alerta a "llegada" |
| 8 | `/ubicaciones` | GET | ListarUbicacionesPatrullasUseCase | IPatrulleroRepository | `ubicaciones_patrullas` | 🔐 Firebase Auth | — |
| 9 | `/ubicaciones/{patrulleroId}` | GET | — (directo) | IPatrulleroRepository | `ubicaciones_patrullas` | 🔐 Firebase Auth | — |
| 10 | `/estadisticas` | GET | — (directo) | IPatrulleroRepository | `ubicaciones_patrullas` | 🔐 Firebase Auth | Calcula % activos (actualización <10min) |

---

## 3️⃣ CONTROLLER: OpenDataController (`/api/opendata/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 11 | `/incidentes` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_incidentes` | ❌ Pública | — |
| 12 | `/incidentes/distrito/{distrito}` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_incidentes` | ❌ Pública | Filtra por Distrito |
| 13 | `/estadisticas` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_agregados` | ❌ Pública | — |
| 14 | `/estadisticas/anio/{anio}` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_agregados` | ❌ Pública | Devuelve 12 meses |
| 15 | `/dashboard` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_agregados` +<br>`open_data_incidentes` | ❌ Pública | Computa tasas de veracidad, promedios |
| 16 | `/descargar/csv` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_incidentes` | ❌ Pública | ✅ Regenera agregados antes de exportar |
| 17 | `/descargar/json` | GET | ObtenerOpenDataUseCase | IOpenDataRepository | `open_data_incidentes` +<br>`open_data_agregados` | ❌ Pública | ✅ Regenera agregados antes de exportar |

---

## 4️⃣ CONTROLLER: AuthController (`/api/auth/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 18 | `/login` | POST | LoginUseCase | IUserRepositoryFirestore | `users` | ❌ Pública | ✅ Verifica Firebase ID Token<br>✅ Actualiza ultimaConexion |

---

## 5️⃣ CONTROLLER: UserController (`/api/user/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 19 | `/buscar` | GET | BuscarUsuarioPorDniUseCase | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Filtra por DNI + Role (opcional) |
| 20 | `/vincular-dispositivo` | POST | VincularDispositivoUseCase | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Actualiza: user.device_id |
| 21 | `/registrar` | POST | RegistrarUsuarioUseCase | IFirebaseAuthService +<br>IUserRepositoryFirestore | `users` (Firestore) | ❌ Pública | ✅ Crea usuario en Firebase Auth<br>✅ Crea doc en users |
| 22 | `/listar` | GET | ListarUsuariosUseCase | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Filtra por Role + Limit |
| 23 | `/editar` | PUT | EditarUsuarioUseCase | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Actualiza campos: nombre, apellido, edad |
| 24 | `/fcm-token` | POST | RegistrarTokenFcmUseCase | IUserRepositoryFirestore +<br>ValidadorDatosService | `users` | 🔐 Firebase Auth | ✅ Valida formato token (20-4096 chars)<br>✅ Actualiza user.FcmToken |
| 25 | `/heartbeat` | POST | — (directo) | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Actualiza: user.ultimaConexion |
| 26 | `/fcm-tokens/diagnostico` | GET | — (directo) | IUserRepositoryFirestore | `users` | 🔐 Firebase Auth | Estadísticas: usuarios con token/sin token por role |

---

## 6️⃣ CONTROLLER: AtestadoPolicialController (`/api/atestado/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 27 | `/` | POST | RegistrarAtestadoPolicialUseCase | IAtestadoPolicialRepository +<br>IOpenDataRepository | `atestados_policiales`<br>`open_data_incidentes`<br>`open_data_agregados` | 🔐 Firebase Auth | ✅ Auto-rellena víctima desde user<br>✅ Convierte a OpenDataIncidente<br>✅ Regenera agregados del mes |
| 28 | `/{id}` | GET | — (directo) | IAtestadoPolicialRepository | `atestados_policiales` | 🔐 Firebase Auth | — |
| 29 | `/alerta/{alertaId}` | GET | — (directo) | IAtestadoPolicialRepository | `atestados_policiales` | 🔐 Firebase Auth | Busca por alertaId |
| 30 | `/patrullero/{uid}` | GET | — (directo) | IAtestadoPolicialRepository | `atestados_policiales` | 🔐 Firebase Auth | Filtra por Patrullero UID |
| 31 | `/rango` | GET | — (directo) | IAtestadoPolicialRepository | `atestados_policiales` | 🔐 Firebase Auth (admin) | Rango de fechas |
| 32 | `/distrito/{distrito}` | GET | — (directo) | IAtestadoPolicialRepository | `atestados_policiales` | 🔐 Firebase Auth (admin) | Filtra por distrito |

---

## 7️⃣ CONTROLLER: DeviceController (`/api/device/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 33 | `/listar-con-vinculo` | GET | ListarDispositivosConVinculoUseCase | IDispositivoRepository +<br>IUserRepositoryFirestore | `dispositivos`<br>`users` | 🔐 Firebase Auth | Merge: dispositivos + estado de vinculación |
| 34 | `/registrar` | POST | RegistrarDispositivoTTSUseCase | IDispositivoRepository +<br>ITTSDeviceService | `dispositivos` | 🔐 Firebase Auth | ✅ Registra en The Things Stack API<br>✅ Guarda en Firestore |
| 35 | `/listar-tts` | GET | ListarDispositivosTTSUseCase | ITTSDeviceService | — | 🔐 Firebase Auth | ✅ Consulta The Things Stack API |

---

## 8️⃣ CONTROLLER: AnalisisController (`/api/analisis/**`)

| # | Endpoint | Método | Use Case | Repository | Colección | Autenticación | Efectos Secundarios |
|---|----------|--------|----------|------------|-----------|----------------|-------------------|
| 36 | `/heatmap` | GET | — (directo) | IOpenDataRepository | `open_data_incidentes` | 🔐 Firebase Auth | Agrupa por lat/lon con precisión configurable |
| 37 | `/tendencias` | GET | — (directo) | IOpenDataRepository | `open_data_incidentes` | 🔐 Firebase Auth | Soporta agregación: hora/día/semana/mes |

---

## 🔗 MAPEO: FIRESTORE COLLECTIONS

```
┌─────────────────────────────────────────────────────────────────┐
│                    FIRESTORE DATABASE                           │
│                   (sis-alert-1e7a7)                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📦 COLLECTIONS (LIVE DATA)                                    │
│  ├─ alertas                      ← Escriben: AlertaController │
│  │                                  Leen: OpenDataController   │
│  │                                                             │
│  ├─ users                        ← UserController CRUD        │
│  │                                  (víctimas, patrulleros)   │
│  │                                                             │
│  ├─ atestados_policiales         ← AtestadoPolicialController │
│  │                                  Disparan: Open Data gen    │
│  │                                                             │
│  ├─ dispositivos                 ← DeviceController           │
│  │                                  (LoRaWAN device registry) │
│  │                                                             │
│  └─ ubicaciones_patrullas        ← PatrullaController         │
│                                      (GPS tracking)            │
│                                                                 │
│  📊 COLLECTIONS (ANONYMIZED ANALYTICS)                         │
│  ├─ open_data_incidentes         ← Auto-generated from        │
│  │                                  atestados via              │
│  │                                  ConvertirAOpenData()       │
│  │                                                             │
│  └─ open_data_agregados          ← Regenerated monthly by     │
│                                      OpenDataRepositoryFirestore│
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 FLUJOS DE DATOS CLAVE

### ✅ FLUJO 1: Alerta desde LoRaWAN
```
1. ESP32/LoRaWAN Device (víctima) → Triggea evento
2. The Things Stack API recibe uplink
3. TTS envía webhook HTTP a: POST /api/alerta/lorawan-webhook
4. AlertaController.RegistrarLorawanWebhook() 
   - Parsea Base64 payload → GPS, Battery, DevEUI
   - Busca user en collection 'users' por device_id = DevEUI
   - Auto-rellena NombreVictima
5. RegistrarAlertaUseCase.EjecutarAsync(alerta)
   - Valida datos con ValidadorDatosService
   - Calcula NivelUrgencia (reactivaciones en 10min)
   - Marca EsRecurrente si YA existe alerta activa
6. IAlertaRepository.SaveAsync(alerta) 
   - Firestore "alertas" collection ← INSERTA nuevo doc
7. EFECTOS SECUNDARIOS ASYNC:
   - ✅ SignalR: _hubContext.Clients.All.SendAsync("RecibirAlerta", {...})
      → Web dashboard actualiza mapa en REAL-TIME
   - ✅ FCM: Consulta users WHERE role="patrullero" AND FcmToken != null
      → IFCMService.EnviarNotificacionMultipleAsync(tokens, notificacion)
      → Móviles de patrulleros reciben PUSH
```

### ✅ FLUJO 2: Patrullero Llega a Escena
```
1. Patrullero App envía: POST /api/patrulla/ubicacion {lat, lon}
2. PatrullaController.ActualizarUbicacion()
   - Extrae patrulleroId del token
   - Busca alerts con Estado="tomada" AND PatrulleroAsignado=patrulleroId
3. ActualizarUbicacionPatrullaUseCase.EjecutarAsync()
   - Guarda en IPatrulleroRepository.SaveAsync(ubicacion)
   - Firestore "ubicaciones_patrullas" collection ← INSERTA GPS
4. LÓGICA CRÍTICA: Haversine Distance Check
   - Para cada alerta encontrada:
     - dist = Haversine(patrullero.lat/lon, alerta.Lat/Lon)
     - Si dist ≤ 30 metros:
       → IAlertaRepository.UpdateFieldsAsync(alertaId, {
           Estado: "llegada",
           FechaLlegada: DateTime.Now
         })
       → Firestore "alertas" collection ← ACTUALIZA documento
```

### ✅ FLUJO 3: Atestado Policial → Open Data
```
1. Patrullero App envía: POST /api/atestado {alertaId, tipoViolencia, nivel, ...}
2. AtestadoPolicialController.RegistrarAtestado()
   - AUTO-RELLENA desde user (if empty):
     SELECT user WHERE device_id = alerta.DeviceId
   - Auto-popula: NombreVictima, DniVictima, EdadAproximada
3. RegistrarAtestadoPolicialUseCase.EjecutarAsync()
   - IAtestadoPolicialRepository.SaveAsync(atestado)
   - Firestore "atestados_policiales" collection ← INSERTA doc
4. TRANSFORMACIÓN A OPEN DATA:
   - AtestadoPolicial.ConvertirAOpenData(alerta)
     - ELIMINA: NombreVictima, DniVictima, DeviceId
     - REDONDEA: Lat/Lon a 3 decimales (~111m precision loss)
     - BUCKETA: EdadAproximada → "menor"|"18-29"|"30-44"|"45-59"|"60+"
     - PRESERVA: TipoViolencia, NivelRiesgo, FechaIncidente, timestamps
   - OpenDataIncidente obj ← Resultado anonimizado
5. IOpenDataRepository.GuardarIncidenteAsync()
   - Firestore "open_data_incidentes" collection ← INSERTA anonimizado
6. REGENERACIÓN DE AGREGADOS:
   - IOpenDataRepository.RegenerarAgregadosAsync(mes, año)
     - Busca ALL atestados en ese mes/año
     - Para each atestado: Convierte a OpenData
     - Computa estadísticas:
       * COUNT by TipoViolencia (4 categorías)
       * COUNT by NivelRiesgo (4 niveles)
       * AVG response time (FechaLlegada - FechaCreacion)
       * AVG resolution time (FechaResuelto - FechaCreacion)
       * Victim age distribution
     - Firestore "open_data_agregados" collection ← INSERTA/ACTUALIZA
```

### ✅ FLUJO 4: Exportar Datos Públicos (CSV/JSON)
```
1. Externa llama: GET /api/opendata/descargar/csv?desde=2025-01-01&hasta=2025-01-31
2. OpenDataController.DescargarCsv()
   - PRIMERO: IOpenDataRepository.RegenerarAgregadosAsync() 
     → Recalcula stats del período (en caso de nuevos atestados)
   - LUEGO: IOpenDataRepository.ObtenerIncidentesPorPeriodoAsync(desde, hasta)
     → Firestore "open_data_incidentes" WHERE FechaIncidente BETWEEN [desde, hasta]
   - Itera cada OpenDataIncidente:
     * Genera fila CSV con 40+ columnas
     * Incluye timestamps, tiempos de respuesta, categorías
     * SIN datos de víctima
   - Response: attachment CSV file
```

---

## 🎯 SEGURIDAD: AUTENTICACIÓN POR ENDPOINT

### 🔓 PÚBLICOS (Sin autenticación)
- `POST /api/alerta/lorawan-webhook` — TTS webhook IPs whitelisted (revisar config)
- `GET /api/opendata/**` — Todo el dashboard público
- `POST /api/user/registrar` — Crear cuenta nueva

### 🔐 PROTEGIDOS (Firebase Auth + Custom Claims)
- `POST /api/alerta/**` — Solo patrulleros/operadores
- `GET /api/alerta/**` — Solo patrulleros/operadores
- `POST /api/patrulla/**` — Solo patrulleros
- `GET /api/patrulla/**` — Solo patrulleros/operadores
- `POST /api/atestado/**` — Solo patrulleros
- `GET /api/user/**` — Solo usuarios autenticados
- `POST /api/device/**` — Solo operadores/admin

**Implementación**: `FirebaseAuthGuardAttribute.OnAuthorizationAsync()`
- Extrae Authorization: Bearer {token}
- Valida con FirebaseAuth.VerifyIdTokenAsync(token)
- Extrae uid + email claims → HttpContext.Items["FirebaseUser"]

---

## 📋 RESUMEN: TABLAS POR RESPONSABILIDAD

### 📝 ENDPOINTS DE ESCRITURA (POST/PUT - Modifican estado)
| Controller | Endpoint | Colección | Efecto |
|-----------|----------|-----------|--------|
| Alerta | `POST /lorawan-webhook` | alertas | Inserta nueva alerta |
| Alerta | `POST /tomar` | alertas | Estado→"tomada", asigna patrullero |
| Alerta | `POST /cambiar-estado` | alertas | Estado transición + timestamp |
| Patrulla | `POST /ubicacion` | ubicaciones_patrullas | Inserta GPS; dispara Haversine→alerta.llegada |
| User | `POST /vincular-dispositivo` | users | Actualiza user.device_id |
| User | `POST /registrar` | users | Inserta nuevo user (Firebase Auth + Firestore) |
| User | `PUT /editar` | users | Actualiza campos usuario |
| User | `POST /fcm-token` | users | Actualiza user.FcmToken |
| Atestado | `POST /` | atestados_policiales | Inserta atestado→auto-genera OpenData |
| Device | `POST /registrar` | dispositivos | Registra device en TTS + Firestore |

### 🔍 ENDPOINTS DE LECTURA (GET - Solo consultan)
| Controller | Endpoint | Colecciones | Operación |
|-----------|----------|-------------|-----------|
| Alerta | `GET /` | alertas | Lista todas |
| Alerta | `GET /rango` | alertas | Filtra por fecha |
| Alerta | `GET /activas` | alertas | Estado != archivada |
| Patrulla | `GET /ubicaciones` | ubicaciones_patrullas | Lista todas |
| Patrulla | `GET /ubicaciones/{id}` | ubicaciones_patrullas | Por ID |
| OpenData | `GET /incidentes` | open_data_incidentes | Anónimizadas |
| OpenData | `GET /estadisticas` | open_data_agregados | Stats mensuales |
| OpenData | `GET /dashboard` | open_data_agregados + incidentes | Resumen global |
| User | `GET /buscar` | users | Por DNI |
| User | `GET /listar` | users | Listado con filtros |
| Atestado | `GET /{id}` | atestados_policiales | Por ID |
| Device | `GET /listar-con-vinculo` | dispositivos + users | Merge estado |

### 🔗 DEPENDENCIAS DE INICIALIZACIÓN (Program.cs)
```csharp
// REPOSITORIES (Singleton pattern)
services.AddSingleton<IAlertaRepository>(new AlertaRepositoryFirestore(firestore));
services.AddSingleton<IUserRepositoryFirestore>(new UserRepositoryFirestore(firestore));
services.AddSingleton<IOpenDataRepository>(new OpenDataRepositoryFirestore(firestore));
services.AddSingleton<IAtestadoPolicialRepository>(new AtestadoPolicialRepositoryFirestore(firestore));
services.AddSingleton<IPatrulleroRepository>(new PatrullaRepositoryFirestore(firestore));
services.AddSingleton<IDispositivoRepository>(new DispositivoRepositoryFirestore(firestore));

// USE CASES (Scoped pattern - nueva instancia por request)
services.AddScoped<RegistrarAlertaUseCase>();
services.AddScoped<LoginUseCase>();
services.AddScoped<RegistrarAtestadoPolicialUseCase>();
// ... 13 más

// EXTERNAL SERVICES
services.AddSingleton<IFCMService>(new FCMService());
services.AddSingleton<IFirebaseAuthService>(new FirebaseAuthService());
services.AddSingleton<ITTSDeviceService>(new TTSDeviceService(...));

// BACKGROUND SERVICES
services.AddHostedService<AlertaExpirationService>(); // Runs every 1 min
```

---

## ⚠️ NOTA: SERVICIOS DUPLICADOS

**IDENTIFICADO**: Dos servicios de expiración de alertas
```
❌ AlertaVencimientoService.cs (NOT in DI container - LEGACY?)
   - Runs every 2 minutes
   - Uses UltimaActivacion timestamp

✅ AlertaExpirationService.cs (IN Program.cs as AddHostedService)
   - Runs every 1 minute
   - Uses FechaCreacion timestamp
   - Actively executing
```

**RECOMENDACIÓN**: Eliminar AlertaVencimientoService; mantener solo AlertaExpirationService

---

## 📊 ESTADÍSTICAS FINALES

- **Total Endpoints**: 37
- **Controladores**: 8
- **Use Cases diferentes**: 16
- **Repositories**: 6
- **Firestore Collections**: 7
- **Endpoints Públicos**: 4 (webhooks + opendata)
- **Endpoints Protegidos (Auth)**: 33

