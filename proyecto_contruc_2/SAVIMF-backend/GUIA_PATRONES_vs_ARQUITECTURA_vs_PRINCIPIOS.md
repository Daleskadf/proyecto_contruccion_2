# 🎓 GUÍA: PATRONES vs ARQUITECTURA vs PRINCIPIOS vs BUENAS PRÁCTICAS

## El problema

Muchas veces confundimos **patrones de diseño** con **patrones arquitectónicos**, **principios** y **buenas prácticas**. Esta guía clarifica las diferencias.

---

## 🏗️ CATEGORÍAS

### 1️⃣ PATRONES DE DISEÑO PUROS (Gang of Four - GoF)

**Qué son**: Soluciones específicas para problemas de diseño recurrentes. **Nivel: bajo** (clases, objetos).

**Características**:
- ✅ Definidos en "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four)
- ✅ Resuelven problemas **micro** (cómo organizar clases)
- ✅ Están basados en estructuras claras (interfaces, herencia, composición)
- ✅ Son **independientes de la tecnología** (aplican en cualquier lenguaje OO)

**En nuestro código**:

| Patrón | Archivo | Propósito | Categoría |
|--------|---------|----------|-----------|
| **Repository** | IAlertaRepository, AlertaRepositoryFirestore | Abstrae acceso a datos | Creacional |
| **Data Mapper** | AlertaRepositoryFirestore | Mapea entre objeto y BD | Estructural |
| **Strategy** | RegistrarAlertaUseCase | Diferentes algoritmos | Comportamiento |

**Familia GoF** (23 patrones totales, nosotros usamos 3):
- **Creacionales** (5): Singleton, Factory, Builder, Prototype, Abstract Factory | ❌ NO en Módulo 1
- **Estructurales** (7): Adapter, Decorator, Facade, **Repository** ✅, **Data Mapper** ✅, Bridge, Composite, Proxy
- **Comportamiento** (11): Observer, Iterator, **Strategy** ✅, State, Template Method, Chain of Responsibility, Command, Interpreter, Mediator, Memento, Visitor

---

### 2️⃣ PATRONES ARQUITECTÓNICOS

**Qué son**: Soluciones para organizar la **estructura macro** de una aplicación. **Nivel: alto** (capas, componentes).

**Características**:
- ✅ Organizan toda la aplicación, no una clase específica
- ✅ Definen cómo se comunican componentes grandes
- ✅ Resuelven problemas de **escalabilidad, separación de concerns**
- ✅ Pueden incluir múltiples patrones GoF

**En nuestro código**:

| Patrón | Descripción | Archivos |
|--------|------------|----------|
| **Clean Architecture** | 4 capas concéntricas | AlertaController → UseCase → Domain → Repository → Firestore |
| **Use Case Pattern** | Cada operación = clase | RegistrarAlertaUseCase, ListarAlertasUseCase, etc. |
| **MVC** (implícito) | Model-View-Controller | AlertaController (Controller), Alerta (Model) |

**Diferencia clave vs GoF**:
- GoF resuelve: "¿Cómo creo objetos sin acoplamiento?"
- Arquitectura resuelve: "¿Cómo organizo TODA mi aplicación?"

---

### 3️⃣ PRINCIPIOS

**Qué son**: Directrices que guían **decisiones de diseño**, no soluciones concretas.

**Características**:
- ✅ Son **valores y directivas**, no "artefactos" de código
- ✅ Pueden apoyarse en patrones GoF
- ✅ Son **generales, aplicables** en múltiples contextos

**En nuestro código**:

| Principio | Expresión | Archivo |
|-----------|-----------|---------|
| **DI (Dependency Injection)** | Inyectar dependencias en constructor | AlertaController, RegistrarAlertaUseCase |
| **DIP (Dependency Inversion)** | Depender de abstracciones, no implementaciones | IAlertaRepository ← depende UseCase |
| **SRP (Single Responsibility)** | Una clase, una razón para cambiar | AlertaController solo orquesta |
| **OCP (Open/Closed)** | Abierto a extensión, cerrado a modificación | AlertaRepositoryFirestore: nueva BD sin código viejo |
| **LSP (Liskov Substitution)** | Subclases intercambiables | IAlertaRepository → AlertaRepositoryFirestore ↔ Otra BD |
| **ISP (Interface Segregation)** | Interfaces específicas, no monolíticas | IAlertaRepository separado de IUserRepository |

**SOLID = 5 principios fundamentales en OOP**

---

### 4️⃣ ENFOQUES DE DISEÑO

**Qué son**: Filosofías o metodologías que **guían el pensamiento** sobre cómo diseñar.

**Características**:
- ✅ Son **mindsets**, no técnicas específicas
- ✅ Pueden invocarse a múltiples niveles
- ✅ Requieren adopción cultural (no es solo código)

**En nuestro código**:

| Enfoque | Descripción | Implementación |
|---------|-------------|-----------------|
| **DDD (Domain-Driven Design)** | Diseño centrado en dominio de negocio | Alerta.cs: métodos como CalcularNivelUrgencia(), DebeArchivarse() que viven en el dominio |
| **TDD (Test-Driven Development)** | Tests primero, código después | RegistrarAlertaUseCaseTests.cs |
| **CLEAN CODE** | Código legible y mantenible | Nombres claros, métodos pequeños, sin magic strings |

**DDD en detalle**:
- Entidades **rich**: contienen lógica, no solo datos
- Agregados: agrupaciones lógicas de entidades
- Bounded Contexts: separación por responsabilidad de negocio

---

### 5️⃣ BUENAS PRÁCTICAS TÉCNICAS

**Qué son**: Técnicas específicas del lenguaje que **mejoran calidad, performance, mantenibilidad**.

**Características**:
- ✅ Son **consejo práctico**, no leyes universales
- ✅ Están validadas por experiencia comunitaria
- ✅ Pueden ser específicas de tecnología (C#, .NET, etc.)

**En nuestro código**:

| Práctica | Técnica | Archivo |
|----------|---------|---------|
| **Enums para type-safety** | Usar `enum AlertaEstado` en lugar de strings | Alerta.cs |
| **Async/Await** | Operaciones no bloqueantes con `async Task` | AlertaController, RegistrarAlertaUseCase |
| **Null Coalescing** | `deviceId ?? string.Empty` | AlertaController |
| **Guard Clauses** | Validar temprano, retornar rápido | AlertaController |
| **Constructor Injection** | Inyectar en constructor, no en método | AlertaController |
| **Immutability** | `readonly` en campos, propiedades sin setters privados | AlertaRepositoryFirestore |

---

## 📊 MATRIZ: CÓMO RELACIONAN

```
Nivel de abstracción (↑ = más abstracto)

┌─────────────────────────────────────────────────────┐
│                                                      │
│  4️⃣ ENFOQUES (DDD, TDD, Clean Code)                 │
│     └─ Filosofías, "cómo pienso sobre código"      │
│                                                      │
│  3️⃣ PRINCIPIOS (SOLID, DI)                          │
│     └─ Directrices, "qué valores guían decisiones"  │
│                                                      │
│  2️⃣ ARQUITECTURA (Clean Arch, Use Case)             │
│     └─ Macro-estructura, "cómo organizo la app"    │
│                                                      │
│  1️⃣ PATRONES GoF (Repository, Data Mapper, etc.)   │
│     └─ Micro-estructura, "cómo creo esta clase"    │
│                                                      │
│  0️⃣ BUENAS PRÁCTICAS (Enum, Async, Null Safety)    │
│     └─ Técnicas específicas, "este language feature"│
│                                                      │
└─────────────────────────────────────────────────────┘
```

**Ejemplo de stack completo**:

```
Enfoque DDD
  ↓ (guiado por)
Principios SOLID (DIP, SRP)
  ↓ (implementado con)
Arquitectura Clean (uso Case Pattern)
  ↓ (que usa)
Patrón Repository (+Data Mapper)
  ↓ (con)
Buena práctica Async/Await
  =
Alerta.cs bien diseñada
```

---

## 🎯 CUANDO USAS CADA UNO

### "¿Cómo hago que AlertaController NO conozca Firestore?"

**Respuesta: Patrones GoF**
```csharp
// Repository Pattern
public interface IAlertaRepository { /* contrato */ }
public class AlertaRepositoryFirestore : IAlertaRepository { /* impl */ }

// AlertaController depende de interfaz, no implementación
public AlertaController(IAlertaRepository repository) { ... }
```

### "¿Cómo organizo mi TODA mi aplicación?"

**Respuesta: Arquitectura**
```
Clean Architecture:
- Controllers (WebAPI layer)
- UseCases (Application layer)
- Entities (Domain layer)
- Repositories (Infrastructure layer)
```

### "¿Debería inyectar o crear con `new`?"

**Respuesta: Principios (DI, DIP)**
```csharp
// ❌ NO: tight coupling
var repo = new AlertaRepositoryFirestore();

// ✅ SÍ: loose coupling via DI
public AlertaController(IAlertaRepository repo) { ... }
```

### "¿Cómo hago que Alerta sea lógica, no solo datos?"

**Respuesta: Enfoque DDD**
```csharp
// Rich Model
public class Alerta
{
    public bool DebeArchivarse() { /* lógica de dominio */ }
    public string CalcularNivelUrgencia() { /* lógica de dominio */ }
}
```

### "¿Cómo evito null reference exception aquí?"

**Respuesta: Buena práctica**
```csharp
// Null coalescing
var deviceId = payload?["device_id"]?.ToString() ?? "unknown";
```

---

## ❌ ERRORES COMUNES DE CLASIFICACIÓN

| Error | Correcto | Por qué |
|-------|----------|---------|
| "Async/Await es un patrón" | Es una **buena práctica** C# | Es una característica del lenguaje |
| "DI es un patrón GoF" | Es un **principio** | No viene del libro GoF, es más moderno |
| "MVC es patrón" | Es **arquitectura** | Organiza capas completas, no una clase |
| "SOLID son patrones" | Son **principios** | Guían decisiones, no son soluciones concretas |
| "DDD es patrón" | Es un **enfoque** | Es una filosofía completa, no una técnica específica |
| "Repository es arquitectura" | Es **patrón GoF** | Afecta una clase/interfaz, no la app entera |

---

## 📋 REFERENCIA RÁPIDA

**Necesito...**

| ... | Busca categoría | Ejemplo | Archivo |
|-----|---|---|---|
| ... desacoplar BD | Patrones GoF | Repository | AlertaController → IAlertaRepository |
| ... organizar capas | Arquitectura | Clean Arch | WebAPI / Application / Domain / Infra |
| ... reducir acoplamiento | Principios | DIP, DI | Constructor injection |
| ... entidades con lógica | Enfoque | DDD | Alerta.cs métodos |
| ... manejar null seguro | Buena práctica | Null coalescing | `?? string.Empty` |
| ... performance sin bloqueos | Buena práctica | Async/Await | `async Task<>` |
| ... type-safety enumerados | Buena práctica | Enums | `AlertaEstado enum` |

---

## 🧠 RESUMEN: JERÁRQUICO

```
┌─────────────────────────────────────────────────────────────┐
│ ENFOQUE (Filosofía global)                                  │
│ └─ DDD: entidades son protagonistas, no tablas BD           │
│                                                              │
│ ├─ PRINCIPIOS (Lo que creo)                                │
│ │  ├─ SOLID: Single, Open, Liskov, Interface, Inversion   │
│ │  ├─ DI: inyectar, no crear                              │
│ │  └─ ...                                                   │
│ │                                                            │
│ ├─ ARQUITECTURA (Cómo organizo)                            │
│ │  └─ Clean: WebAPI → Application → Domain → Infrastructure│
│ │     └─ Use Case: cada operación es clase                 │
│ │                                                            │
│ ├─ PATRONES GoF (Cómo implemento)                          │
│ │  ├─ Repository: abstracción de datos                     │
│ │  ├─ Data Mapper: mapeo Alerta ↔ Firestore               │
│ │  ├─ Strategy: múltiples algoritmos                       │
│ │  └─ ...                                                   │
│ │                                                            │
│ └─ BUENAS PRÁCTICAS (Detalles técnicos)                    │
│    ├─ Enums en lugar de strings                            │
│    ├─ Async/Await para I/O                                 │
│    ├─ Null coalescing para seguridad                       │
│    └─ ...                                                   │
│                                                              │
│ = Código bien diseñado en Módulo 1                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 📖 REFERENCIAS

**Patrones GoF**:
- [Refactoring Guru - Design Patterns](https://refactoring.guru/design-patterns)
- "Design Patterns: Elements of Reusable Object-Oriented Software" (Gang of Four, 1994)

**Principios SOLID**:
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- Robert C. Martin (Uncle Bob)

**Arquitectura**:
- "Clean Architecture" Robert C. Martin
- "Patterns of Enterprise Application Architecture" - Martin Fowler

**DDD**:
- "Domain-Driven Design" Eric Evans
- "Implementing Domain-Driven Design" Vaughn Vernon

**Buenas Prácticas C#/.NET**:
- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Effective C# (Bill Wagner)

---

**ACTUALIZACIÓN**: Documento ANALISIS_BUENAS_PRACTICAS_MODULO1.md ha sido refactorizado con esta clasificación correcta.

