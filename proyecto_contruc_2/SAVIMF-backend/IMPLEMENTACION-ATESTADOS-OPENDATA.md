# 📘 Implementación Backend - Atestados Policiales + Open Data

## 🎯 Arquitectura Implementada

### **Clean Architecture + Design Patterns**
- ✅ **Domain Layer**: Entidades puras + interfaces (Repository, Factory, Strategy)
- ✅ **Application Layer**: Use Cases (CQRS - Command Query Responsibility Segregation)
- ✅ **Infrastructure Layer**: Repositorios Firestore (Data Mapper)
- ✅ **WebAPI Layer**: Controllers REST + DTOs

---

## 📂 Estructura de Archivos Creados

### **1️⃣ Domain Layer (`Domain/Entities`)**
```
✅ AtestadoPolicial.cs         - Entidad principal con datos completos del incidente
✅ OpenDataIncidente.cs         - Value Object inmutable (datos anónimos)
✅ OpenDataAgregado.cs          - Aggregate Root para estadísticas por distrito/mes
```

**Patrones Aplicados:**
- **Entity (DDD)**: `AtestadoPolicial` con identidad y comportamiento
- **Value Object**: `OpenDataIncidente` inmutable sin ID
- **Factory Method**: `ConvertirAOpenData()` crea instancia anónima
- **Strategy**: `ObtenerRangoEdad()` para anonimización de edad
- **Aggregate Root**: `OpenDataAgregado` con método `AgregarIncidente()`

---

### **2️⃣ Domain Interfaces (`Domain/Interfaces`)**
```
✅ IAtestadoPolicialRepository.cs  - CRUD + consultas por alerta/patrullero/distrito/fechas
✅ IOpenDataRepository.cs          - Gestión de incidentes y agregados
```

---

### **3️⃣ Application Layer (`Application/UseCases`)**
```
✅ RegistrarAtestadoPolicialUseCase.cs  - Registra atestado + genera Open Data automáticamente
✅ ObtenerOpenDataUseCase.cs            - Consultas de datos públicos (Query CQRS)
```

**Patrón CQRS:**
- **Command**: `RegistrarAtestadoPolicialUseCase` (escritura + side effects)
- **Query**: `ObtenerOpenDataUseCase` (solo lectura, sin modificar estado)

---

### **4️⃣ Infrastructure Layer (`Infrastructure/Persistence`)**
```
✅ AtestadoPolicialRepositoryFirestore.cs  - Implementación Firestore para atestados
✅ OpenDataRepositoryFirestore.cs          - Implementación Firestore para Open Data
```

**Patrones:**
- **Repository**: Abstrae persistencia de Firestore
- **Data Mapper**: Métodos `MapearDesdeFirestore()` convierten DocumentSnapshot → Entidad

**Colecciones Firestore:**
- `atestados_policiales`: Datos completos (incluye info sensible)
- `open_data_incidentes`: Incidentes individuales anónimos
- `open_data_agregados`: Estadísticas por distrito/mes

---

### **5️⃣ WebAPI Layer (`WebAPI/Controllers`)**
```
✅ AtestadoPolicialController.cs    - Endpoints protegidos (solo autenticados)
✅ OpenDataController.cs             - Endpoints públicos + descarga CSV/JSON
✅ RegistrarAtestadoRequestDto.cs    - DTO para request desde app móvil
```

---

## 🔗 Endpoints Creados

### **🔒 Atestados Policiales** (Requieren autenticación)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/atestadopolicial` | Registra atestado desde app móvil |
| `GET` | `/api/atestadopolicial/{id}` | Obtiene atestado por ID |
| `GET` | `/api/atestadopolicial/alerta/{alertaId}` | Obtiene atestado de una alerta |
| `GET` | `/api/atestadopolicial/patrullero/{uid}` | Atestados de un patrullero |
| `GET` | `/api/atestadopolicial/rango?fechaInicio=...&fechaFin=...` | Filtro por fechas (admin) |
| `GET` | `/api/atestadopolicial/distrito/{distrito}` | Filtro por distrito (admin) |

### **🌐 Open Data** (Acceso público)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/opendata/incidentes?anio=2025&mes=11` | Incidentes individuales |
| `GET` | `/api/opendata/incidentes/distrito/{distrito}` | Incidentes de un distrito |
| `GET` | `/api/opendata/estadisticas?distrito=...&anio=...&mes=...` | Estadísticas agregadas |
| `GET` | `/api/opendata/estadisticas/anio/{anio}` | Todas las estadísticas de un año |
| `GET` | `/api/opendata/dashboard` | Dashboard global con métricas |
| `GET` | `/api/opendata/descargar/csv?anio=...&mes=...` | Descarga CSV |
| `GET` | `/api/opendata/descargar/json?anio=...` | Descarga JSON agregado |
| `POST` | `/api/opendata/regenerar?anio=...&mes=...` | Recalcula estadísticas (admin) |

---

## 🔐 Datos Sensibles vs Open Data

### ❌ **NO van a Open Data** (privados):
- ❌ Nombre completo de la víctima
- ❌ DNI de la víctima
- ❌ DevEUI del dispositivo
- ❌ Device ID
- ❌ Correos electrónicos
- ❌ Coordenadas exactas (solo redondeadas a 3 decimales ≈ 111 metros)

### ✅ **SÍ van a Open Data** (anónimos):
- ✅ Fecha/hora del incidente
- ✅ Coordenadas redondeadas (lat/lng con 3 decimales)
- ✅ Distrito
- ✅ Tipo de violencia (física, psicológica, sexual, económica)
- ✅ Nivel de riesgo (bajo, medio, alto, crítico)
- ✅ Si la alerta fue verídica o falsa
- ✅ Rango de edad de víctima (`menor`, `18-29`, `30-44`, `45-59`, `60+`)
- ✅ Si requirió ambulancia
- ✅ Si requirió refuerzo policial

---

## 🚀 Flujo de Negocio

### **1. Patrullero resuelve alerta en app móvil**
```
App Flutter → POST /api/atestadopolicial
```

### **2. Backend ejecuta `RegistrarAtestadoPolicialUseCase`**
```
1️⃣ Valida que el atestado sea válido (EsValido())
2️⃣ Verifica que no exista ya un atestado para esta alerta
3️⃣ Verifica que la alerta exista y esté en estado "resuelto"
4️⃣ Guarda atestado en `atestados_policiales`
5️⃣ Convierte a Open Data anónimo → guarda en `open_data_incidentes`
6️⃣ Actualiza estadísticas agregadas en `open_data_agregados`
```

### **3. Web Dashboard consulta Open Data**
```
React Web → GET /api/opendata/estadisticas
React Web → GET /api/opendata/descargar/csv
```

---

## 📊 Ejemplo de Open Data JSON

```json
{
  "metadata": {
    "anio": 2025,
    "fechaGeneracion": "2025-11-18T10:30:00Z",
    "fuente": "Sistema SAVIMF - Tacna, Perú",
    "totalDistritos": 12,
    "totalIncidentes": 347
  },
  "estadisticas": [
    {
      "distrito": "Centro",
      "periodo": "2025-11",
      "totalIncidentes": 89,
      "tasaVeracidad": 91.5,
      "tiposViolencia": {
        "violenciaFisica": 45,
        "violenciaPsicologica": 28,
        "violenciaSexual": 10,
        "violenciaEconomica": 6
      },
      "nivelesRiesgo": {
        "riesgoBajo": 12,
        "riesgoMedio": 34,
        "riesgoAlto": 28,
        "riesgoCritico": 15
      },
      "distribucionEdades": {
        "menores": 5,
        "jovenes18_29": 34,
        "adultos30_44": 28,
        "adultos45_59": 18,
        "mayores60": 4
      },
      "ubicacionCentral": {
        "latitud": -18.013,
        "longitud": -70.245
      }
    }
  ]
}
```

---

## ✅ Registro en Program.cs

```csharp
// Repositorios
builder.Services.AddScoped<IAtestadoPolicialRepository, AtestadoPolicialRepositoryFirestore>();
builder.Services.AddScoped<IOpenDataRepository, OpenDataRepositoryFirestore>();

// Use Cases
builder.Services.AddScoped<RegistrarAtestadoPolicialUseCase>();
builder.Services.AddScoped<ObtenerOpenDataUseCase>();
```

---

## 🎨 Patrones de Diseño Aplicados

1. **Repository Pattern** → Abstrae Firestore
2. **Use Case Pattern** (Clean Architecture) → Lógica de negocio aislada
3. **CQRS** → Separación Command/Query
4. **Factory Method** → `ConvertirAOpenData()`
5. **Strategy** → Anonimización de datos
6. **Aggregate Root** → `OpenDataAgregado`
7. **Value Object** → `OpenDataIncidente`
8. **DTO Pattern** → `RegistrarAtestadoRequestDto`
9. **Data Mapper** → `MapearDesdeFirestore()`
10. **Dependency Injection** → Todo inyectado en constructores

---

## 📝 Próximos Pasos

### ✅ Backend COMPLETADO
### 🔄 Ahora implementar:
1. **App Flutter**: Formulario de atestado policial
2. **React Web**: 
   - Módulo de Reportes Internos
   - Módulo de Open Data con descarga CSV/JSON

---

## 🧪 Testing

### Probar endpoints con curl:

```bash
# 1. Registrar atestado (requiere token JWT)
curl -X POST http://localhost:5000/api/atestadopolicial \
  -H "Authorization: Bearer {FIREBASE_ID_TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "alertaId": "alerta123",
    "fechaIncidente": "2025-11-18T08:30:00Z",
    "latitud": -18.0133,
    "longitud": -70.2452,
    "distrito": "Centro",
    "tipoViolencia": "fisica",
    "nivelRiesgo": "alto",
    "alertaVeridica": true,
    "descripcionHechos": "Descripción del incidente...",
    "nombreVictima": "María García",
    "dniVictima": "12345678",
    "edadAproximada": 32,
    "requirioAmbulancia": false,
    "requirioRefuerzo": true
  }'

# 2. Consultar Open Data (público)
curl http://localhost:5000/api/opendata/dashboard

# 3. Descargar CSV
curl http://localhost:5000/api/opendata/descargar/csv?anio=2025&mes=11 -o open_data.csv
```

---

**✨ Backend implementado con Clean Architecture + Design Patterns + SOLID** ✨
