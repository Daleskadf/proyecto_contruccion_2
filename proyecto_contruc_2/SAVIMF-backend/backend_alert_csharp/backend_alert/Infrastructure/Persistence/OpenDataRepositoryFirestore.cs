using Google.Cloud.Firestore;
using backend_alert.Domain.Entities;
using backend_alert.Domain.Interfaces;
using Domain.Interfaces; // Para IAlertaRepository
using System.Text.Json;
using Domain.Entities; // Para Alerta
using System.Linq;
using System.Text.Json;

namespace backend_alert.Infrastructure.Persistence;

/// <summary>
/// Implementación del repositorio de Open Data en Firestore - VERSION CONSOLIDADA
/// USA UNA SOLA COLECCIÓN: "open_data" (elimina las 2 colecciones antiguas)
/// LEE DE MÚLTIPLES FUENTES: alertas + atestados_policiales para datos completos
/// Pattern: Repository + Data Mapper
/// </summary>
public class OpenDataRepositoryFirestore : IOpenDataRepository
{
    private readonly FirestoreDb _firestoreDb;
    private readonly IAtestadoPolicialRepository _atestadoRepository;
    private readonly IAlertaRepository _alertaRepository;
    
    // ✅ COLECCIÓN ÚNICA CONSOLIDADA
    private const string OPEN_DATA_COLLECTION = "open_data";

    public OpenDataRepositoryFirestore(
        FirestoreDb firestoreDb,
        IAtestadoPolicialRepository atestadoRepository,
        IAlertaRepository alertaRepository)
    {
        _firestoreDb = firestoreDb;
        _atestadoRepository = atestadoRepository;
        _alertaRepository = alertaRepository;
    }

    public async Task GuardarIncidenteAsync(OpenDataIncidente incidente)
    {
    // Calcular fechaRegistroOpenData localmente: no modificar la instancia (propiedad init-only)
    DateTime fechaRegistro = incidente.FechaRegistroOpenData == default ? DateTime.UtcNow : incidente.FechaRegistroOpenData;

    var data = new Dictionary<string, object?>
    {
            // ⏰ TIEMPOS DEL CICLO DE VIDA
            ["fechaCreacionAlerta"] = incidente.FechaCreacionAlerta.HasValue 
                ? (object)Timestamp.FromDateTime(incidente.FechaCreacionAlerta.Value.ToUniversalTime()) 
                : null,
            ["fechaTomadaAlerta"] = incidente.FechaTomadaAlerta.HasValue 
                ? (object)Timestamp.FromDateTime(incidente.FechaTomadaAlerta.Value.ToUniversalTime()) 
                : null,
            ["fechaLlegada"] = incidente.FechaLlegada.HasValue 
                ? (object)Timestamp.FromDateTime(incidente.FechaLlegada.Value.ToUniversalTime()) 
                : null,
            ["fechaAtendida"] = incidente.FechaAtendida.HasValue 
                ? (object)Timestamp.FromDateTime(incidente.FechaAtendida.Value.ToUniversalTime()) 
                : null,
            ["fechaResuelto"] = incidente.FechaResuelto.HasValue 
                ? (object)Timestamp.FromDateTime(incidente.FechaResuelto.Value.ToUniversalTime()) 
                : null,
            
            // 📊 TIEMPOS CALCULADOS
            ["tiempoRespuestaMinutos"] = incidente.TiempoRespuestaMinutos,
            ["tiempoLlegadaMinutos"] = incidente.TiempoLlegadaMinutos,
            ["tiempoAtencionMinutos"] = incidente.TiempoAtencionMinutos,
            ["tiempoTotalMinutos"] = incidente.TiempoTotalMinutos,
            
            // 📅 Temporal del incidente
            ["fechaIncidente"] = Timestamp.FromDateTime(incidente.FechaIncidente.ToUniversalTime()),
            ["anio"] = incidente.Anio,
            ["mes"] = incidente.Mes,
            ["dia"] = incidente.Dia,
            ["mesNombre"] = incidente.MesNombre,
            ["diaSemana"] = incidente.DiaSemana,
            ["diaSemanaNombre"] = incidente.DiaSemanaNombre,
            ["horaDelDia"] = incidente.HoraDelDia,
            ["minutoDelDia"] = incidente.MinutoDelDia,
            
            // 📊 METADATA DE LA ALERTA
            ["cantidadActivaciones"] = incidente.CantidadActivaciones,
            ["esRecurrente"] = incidente.EsRecurrente,
            ["estadoFinal"] = incidente.EstadoFinal,
            ["nivelUrgencia"] = incidente.NivelUrgencia,
            
            // 📍 Ubicación anonimizada
            ["latitudRedondeada"] = incidente.LatitudRedondeada,
            ["longitudRedondeada"] = incidente.LongitudRedondeada,
            ["distrito"] = incidente.Distrito,
            
            // 🔍 Características
            ["tipoViolencia"] = incidente.TipoViolencia,
            ["nivelRiesgo"] = incidente.NivelRiesgo,
            ["alertaVeridica"] = incidente.AlertaVeridica,
            
            // 👤 Demografía anonimizada
            ["edadVictimaRango"] = incidente.EdadVictimaRango,
            ["generoVictima"] = incidente.GeneroVictima,
            
            // 🚨 Recursos
            ["requirioAmbulancia"] = incidente.RequirioAmbulancia,
            ["requirioRefuerzo"] = incidente.RequirioRefuerzo,
            ["victimaTrasladadaComisaria"] = incidente.VictimaTrasladadaComisaria,
            
            // 🔋 Dispositivo (anonimizado)
            ["bateriaNivel"] = incidente.BateriaNivel,
            ["dispositivoTipo"] = incidente.DispositivoTipo,
            
            // 📅 Metadata
            ["fechaRegistroOpenData"] = Timestamp.FromDateTime(fechaRegistro.ToUniversalTime())
        };
        // Guardar con el Id del incidente para poder reemplazar registros existentes
        var docId = incidente.Id ?? Guid.NewGuid().ToString();
        var docRef = _firestoreDb.Collection(OPEN_DATA_COLLECTION).Document(docId);

        // Log detallado: mostrar qué se está guardando (JSON del incidente) y la ruta del documento
        try
        {
            var incidenteJson = JsonSerializer.Serialize(incidente, new JsonSerializerOptions { WriteIndented = false });
            Console.WriteLine($"🔁 Guardando documento 'open_data/{docId}': {incidenteJson}");
        }
        catch (Exception exLog)
        {
            Console.WriteLine($"⚠️ Error serializando OpenDataIncidente para log: {exLog.Message}");
        }

        await docRef.SetAsync(data);
    }

    public async Task<List<OpenDataIncidente>> ObtenerIncidentesPorPeriodoAsync(int anio, int? mes = null)
    {
        try
        {
            Query query = _firestoreDb.Collection(OPEN_DATA_COLLECTION)
                .WhereEqualTo("anio", anio);

            if (mes.HasValue)
            {
                query = query.WhereEqualTo("mes", mes.Value);
            }

            query = query.OrderByDescending("fechaIncidente");

            var snapshot = await query.GetSnapshotAsync();
            Console.WriteLine($"📊 Open Data: Encontrados {snapshot.Documents.Count} documentos para {anio}-{mes?.ToString() ?? "todos"}");
            
            var incidentes = new List<OpenDataIncidente>();

            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var incidente = MapearIncidenteDesdeFirestore(doc);
                    if (incidente != null)
                        incidentes.Add(incidente);
                }
                catch (Exception exDoc)
                {
                    Console.WriteLine($"❌ Error mapeando documento {doc.Id}: {exDoc.Message}");
                }
            }

            Console.WriteLine($"✅ Open Data: Mapeados {incidentes.Count} incidentes correctamente");
            return incidentes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en ObtenerIncidentesPorPeriodoAsync: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<OpenDataIncidente>> ObtenerIncidentesPorDistritoAsync(string distrito)
    {
        var query = _firestoreDb.Collection(OPEN_DATA_COLLECTION)
            .WhereEqualTo("distrito", distrito)
            .OrderByDescending("fechaIncidente");

        var snapshot = await query.GetSnapshotAsync();
        var incidentes = new List<OpenDataIncidente>();

        foreach (var doc in snapshot.Documents)
        {
            var incidente = MapearIncidenteDesdeFirestore(doc);
            if (incidente != null)
                incidentes.Add(incidente);
        }

        return incidentes;
    }

    public async Task GuardarAgregadoAsync(OpenDataAgregado agregado)
    {
        // ⚠️ DEPRECATED: Los agregados ahora se calculan on-the-fly desde open_data
        // Mantener solo para compatibilidad temporal
        await Task.CompletedTask;
    }

    public async Task<OpenDataAgregado?> ObtenerAgregadoAsync(string distrito, int anio, int mes)
    {
        // Calcular agregado on-the-fly desde colección open_data
        var incidentes = await ObtenerIncidentesPorPeriodoAsync(anio, mes);
        var incidentesDistrito = incidentes.Where(i => i.Distrito == distrito).ToList();

        if (!incidentesDistrito.Any())
            return null;

        var agregado = new OpenDataAgregado(distrito, anio, mes);
        
        foreach (var inc in incidentesDistrito)
        {
            agregado.TotalIncidentes++;
            if (inc.AlertaVeridica) agregado.IncidentesVeridicos++;
            
            switch (inc.TipoViolencia.ToLower())
            {
                case "fisica": agregado.ViolenciaFisica++; break;
                case "psicologica": agregado.ViolenciaPsicologica++; break;
                case "sexual": agregado.ViolenciaSexual++; break;
                case "economica": agregado.ViolenciaEconomica++; break;
            }

            switch (inc.NivelRiesgo.ToLower())
            {
                case "bajo": agregado.RiesgoBajo++; break;
                case "medio": agregado.RiesgoMedio++; break;
                case "alto": agregado.RiesgoAlto++; break;
                case "critico": agregado.RiesgoCritico++; break;
            }
        }

        return agregado;
    }

    public async Task<List<OpenDataAgregado>> ObtenerAgregadosPorAnioAsync(int anio)
    {
        var incidentes = await ObtenerIncidentesPorPeriodoAsync(anio);
        
        var grupos = incidentes
            .GroupBy(i => new { i.Distrito, i.Mes })
            .Select(g => new { g.Key.Distrito, g.Key.Mes, Incidentes = g.ToList() });

        var agregados = new List<OpenDataAgregado>();

        foreach (var grupo in grupos)
        {
            var agregado = await ObtenerAgregadoAsync(grupo.Distrito, anio, grupo.Mes) 
                ?? new OpenDataAgregado(grupo.Distrito, anio, grupo.Mes);
            agregados.Add(agregado);
        }

        return agregados.OrderBy(a => a.Distrito).ThenBy(a => a.Mes).ToList();
    }

    public async Task<Dictionary<string, object>> ObtenerEstadisticasGlobalesAsync()
    {
        try
        {
            var anioActual = DateTime.UtcNow.Year;
            Console.WriteLine($"📊 Obteniendo estadísticas globales para año {anioActual}");
            
            var incidentes = await ObtenerIncidentesPorPeriodoAsync(anioActual);
            Console.WriteLine($"📊 Incidentes encontrados: {incidentes.Count}");

            if (incidentes.Count == 0)
            {
                Console.WriteLine("⚠️ No hay datos disponibles para estadísticas");
                return new Dictionary<string, object>
                {
                    { "anio", anioActual },
                    { "totalIncidentes", 0 },
                    { "totalVeridicos", 0 },
                    { "porcentajeVeridicos", 0.0 },
                    { "tipoMasFrecuente", "N/A" },
                    { "riesgoPredominante", "N/A" },
                    { "distritoMasAfectado", "N/A" },
                    { "tiempoPromedioRespuesta", 0.0 },
                    { "tiempoPromedioResolucion", 0.0 },
                    { "alertasRecurrentes", 0 },
                    { "mensaje", "No hay datos disponibles" }
                };
            }

        var totalIncidentes = incidentes.Count;
        var totalVeridicos = incidentes.Count(i => i.AlertaVeridica);
        var tasaVeracidad = (double)totalVeridicos / totalIncidentes * 100;

        var tiemposRespuesta = incidentes.Where(i => i.TiempoRespuestaMinutos.HasValue)
            .Select(i => i.TiempoRespuestaMinutos!.Value).ToList();
        var tiemposResolucion = incidentes.Where(i => i.TiempoTotalMinutos.HasValue)
            .Select(i => i.TiempoTotalMinutos!.Value).ToList();

        var tiempoPromedioRespuesta = tiemposRespuesta.Any() ? tiemposRespuesta.Average() : 0;
        var tiempoPromedioResolucion = tiemposResolucion.Any() ? tiemposResolucion.Average() : 0;

        var alertasRecurrentes = incidentes.Count(i => i.EsRecurrente);
        var alertasUrgenciaAlta = incidentes.Count(i => i.NivelUrgencia == "alta" || i.NivelUrgencia == "critica");

        var distritoMasAfectado = incidentes
            .GroupBy(i => i.Distrito)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var tiposViolencia = new Dictionary<string, int>
        {
            { "Física", incidentes.Count(i => i.TipoViolencia.ToLower() == "fisica") },
            { "Psicológica", incidentes.Count(i => i.TipoViolencia.ToLower() == "psicologica") },
            { "Sexual", incidentes.Count(i => i.TipoViolencia.ToLower() == "sexual") },
            { "Económica", incidentes.Count(i => i.TipoViolencia.ToLower() == "economica") }
        };

        var nivelesRiesgo = new Dictionary<string, int>
        {
            { "Crítico", incidentes.Count(i => i.NivelRiesgo.ToLower() == "critico") },
            { "Alto", incidentes.Count(i => i.NivelRiesgo.ToLower() == "alto") },
            { "Medio", incidentes.Count(i => i.NivelRiesgo.ToLower() == "medio") },
            { "Bajo", incidentes.Count(i => i.NivelRiesgo.ToLower() == "bajo") }
        };

        return new Dictionary<string, object>
        {
            { "totalIncidentes", totalIncidentes },
            { "totalVeridicos", totalVeridicos },
            { "porcentajeVeridicos", Math.Round(tasaVeracidad, 2) },
            { "tipoMasFrecuente", tiposViolencia.MaxBy(t => t.Value).Key },
            { "riesgoPredominante", nivelesRiesgo.MaxBy(r => r.Value).Key },
            { "distritoMasAfectado", distritoMasAfectado?.Key ?? "N/A" },
            { "tiempoPromedioRespuesta", Math.Round(tiempoPromedioRespuesta, 2) },
            { "tiempoPromedioResolucion", Math.Round(tiempoPromedioResolucion, 2) },
            { "alertasRecurrentes", alertasRecurrentes },
            { "alertasUrgenciaAlta", alertasUrgenciaAlta },
            { "porDistrito", incidentes.GroupBy(i => i.Distrito).ToDictionary(g => g.Key, g => g.Count()) },
            { "porTipo", tiposViolencia },
            { "porRiesgo", nivelesRiesgo }
        };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en ObtenerEstadisticasGlobalesAsync: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
            throw;
        }
    }

    public async Task RegenerarAgregadosAsync(int anio, int mes)
    {
        try
        {
            Console.WriteLine($"🔄 Iniciando regeneración de Open Data para {anio}-{mes:D2}");
            
            // 🔥 NUEVO: Regenera colección open_data leyendo de alertas + atestados
            var fechaInicio = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);
            
            Console.WriteLine($"📅 Rango de fechas: {fechaInicio:yyyy-MM-dd} a {fechaFin:yyyy-MM-dd}");
            
            var atestados = await _atestadoRepository.ObtenerPorRangoFechasAsync(fechaInicio, fechaFin);
            Console.WriteLine($"📋 Encontrados {atestados.Count()} atestados");

            // 🔥 CAMBIO: Solo eliminar registros que tienen atestado (casos resueltos)
            // NO borrar alertas vencidas/no_resueltas que fueron creadas por AlertaExpirationService
            try
            {
                // Obtener IDs de atestados para solo eliminar sus registros OpenData previos
                var atestadoIds = atestados.Select(a => a.Id).ToHashSet();
                
                var queryEliminar = _firestoreDb.Collection(OPEN_DATA_COLLECTION)
                    .WhereGreaterThanOrEqualTo("fechaIncidente", fechaInicio.ToUniversalTime())
                    .WhereLessThanOrEqualTo("fechaIncidente", fechaFin.ToUniversalTime())
                    .WhereEqualTo("estadoFinal", "resuelto"); // 🔥 SOLO eliminar casos resueltos

                var snapEliminar = await queryEliminar.GetSnapshotAsync();
                Console.WriteLine($"🧹 Eliminando {snapEliminar.Count} documentos RESUELTOS previos en open_data para {anio}-{mes:D2}");
                Console.WriteLine($"⚠️ MANTENIENDO alertas vencidas/no_resueltas (creadas por AlertaExpirationService)");
                
                foreach (var doc in snapEliminar.Documents)
                {
                    try
                    {
                        await doc.Reference.DeleteAsync();
                    }
                    catch (Exception exDel)
                    {
                        Console.WriteLine($"⚠️ Error eliminando documento {doc.Id}: {exDel.Message}");
                    }
                }
            }
            catch (Exception exDelQuery)
            {
                Console.WriteLine($"⚠️ Error preparando limpieza de open_data: {exDelQuery.Message}");
            }

            int guardados = 0;
            foreach (var atestado in atestados)
            {
                try
                {
                    Alerta? alerta = null;
                    if (!string.IsNullOrEmpty(atestado.AlertaId))
                    {
                        alerta = await _alertaRepository.ObtenerPorIdAsync(atestado.AlertaId);
                        Console.WriteLine($"🔗 Atestado {atestado.Id} → Alerta {atestado.AlertaId} {(alerta != null ? "encontrada" : "NO encontrada")}");
                    }

                    // Diagnostic logging: print alerta and atestado timestamps/metadata to debug missing fields
                    try
                    {
                        Console.WriteLine("--- DEBUG: fuente datos para OpenData ---");
                        Console.WriteLine($"Atestado.Id={atestado.Id} AlertaId={atestado.AlertaId}");
                        Console.WriteLine($"Atestado.FechaCreacion={atestado.FechaCreacion.ToDateTime():u} FechaIncidente={atestado.FechaIncidente.ToDateTime():u}");
                        Console.WriteLine($"Atestado.Lat={atestado.Latitud} Lon={atestado.Longitud} EdadAprox={atestado.EdadAproximada}");

                        if (alerta == null)
                        {
                            Console.WriteLine("Alerta: NULL (no existe en BD o no fue encontrada)");
                        }
                        else
                        {
                            // Guardar estado anterior para detectar qué se rellenó
                            var hadFechaTomada = alerta.FechaTomada.HasValue;
                            var hadFechaLlegada = alerta.FechaLlegada.HasValue;
                            var hadFechaAtendida = alerta.FechaAtendida.HasValue;
                            var hadFechaResuelto = alerta.FechaResuelto.HasValue;
                            var hadBateria = alerta.Bateria > 0;
                            var hadCantAct = alerta.CantidadActivaciones > 0;
                            var hadNivelUrg = !string.IsNullOrWhiteSpace(alerta.NivelUrgencia);

                            bool intentoHistorico = false;
                            try
                            {
                                bool necesitaRelleno = !hadFechaTomada || !hadFechaLlegada || !hadFechaAtendida || !hadBateria || !hadCantAct || !hadNivelUrg;
                                if (necesitaRelleno && !string.IsNullOrWhiteSpace(alerta.DevEUI))
                                {
                                    intentoHistorico = true;
                                    // Buscar alertas del mismo device en los últimos 7 días como fuente de datos
                                    var desde = alerta.FechaCreacion.AddDays(-7);
                                    var historico = await _alertaRepository.BuscarAlertasPorDeviceIdYTiempoAsync(alerta.DevEUI, desde);
                                    if (historico != null && historico.Any())
                                    {
                                        var fuente = historico.FirstOrDefault(h => h.Id != alerta.Id);
                                        if (fuente != null)
                                        {
                                            // Rellenar solo los campos que faltan (en memoria)
                                            if (!hadFechaTomada && fuente.FechaTomada.HasValue)
                                                alerta.FechaTomada = fuente.FechaTomada;
                                            if (!hadFechaLlegada && fuente.FechaLlegada.HasValue)
                                                alerta.FechaLlegada = fuente.FechaLlegada;
                                            if (!hadFechaAtendida && fuente.FechaAtendida.HasValue)
                                                alerta.FechaAtendida = fuente.FechaAtendida;
                                            if (!hadFechaResuelto && fuente.FechaResuelto.HasValue)
                                                alerta.FechaResuelto = fuente.FechaResuelto;
                                            if (!hadBateria && fuente.Bateria > 0)
                                                alerta.Bateria = fuente.Bateria;
                                            if (!hadCantAct && fuente.CantidadActivaciones > 0)
                                                alerta.CantidadActivaciones = fuente.CantidadActivaciones;
                                            if (!hadNivelUrg && !string.IsNullOrWhiteSpace(fuente.NivelUrgencia))
                                                alerta.NivelUrgencia = fuente.NivelUrgencia;
                                        }
                                    }
                                }
                            }
                            catch (Exception exHist)
                            {
                                Console.WriteLine($"⚠️ Error buscando alertas históricas para rellenar campos: {exHist.Message}");
                            }

                            Console.WriteLine($"Alerta.Id={alerta.Id} FechaCreacion={alerta.FechaCreacion:u}");
                            Console.WriteLine($"Alerta.FechaTomada={(alerta.FechaTomada.HasValue ? alerta.FechaTomada.Value.ToString("u") : "NULL")}");
                            Console.WriteLine($"Alerta.FechaLlegada={(alerta.FechaLlegada.HasValue ? alerta.FechaLlegada.Value.ToString("u") : "NULL")}");
                            Console.WriteLine($"Alerta.FechaAtendida={(alerta.FechaAtendida.HasValue ? alerta.FechaAtendida.Value.ToString("u") : "NULL")}");
                            Console.WriteLine($"Alerta.FechaResuelto={(alerta.FechaResuelto.HasValue ? alerta.FechaResuelto.Value.ToString("u") : "NULL")}");
                            Console.WriteLine($"Alerta.Bateria={alerta.Bateria} CantAct={alerta.CantidadActivaciones} NivelUrg={alerta.NivelUrgencia} Estado={alerta.Estado}");
                            Console.WriteLine($"HistoricoIntentado={intentoHistorico} RellenoRealizado={(!hadFechaTomada && alerta.FechaTomada.HasValue) || (!hadFechaLlegada && alerta.FechaLlegada.HasValue) || (!hadFechaAtendida && alerta.FechaAtendida.HasValue) || (!hadFechaResuelto && alerta.FechaResuelto.HasValue) || (!hadBateria && alerta.Bateria>0) || (!hadCantAct && alerta.CantidadActivaciones>0) || (!hadNivelUrg && !string.IsNullOrWhiteSpace(alerta.NivelUrgencia))}");
                        }
                        Console.WriteLine("--- /DEBUG ---");
                    }
                    catch (Exception dbgEx)
                    {
                        Console.WriteLine($"⚠️ Error log diagnóstico: {dbgEx.Message}");
                    }

                    var incidenteOpenData = atestado.ConvertirAOpenData(alerta);
                    await GuardarIncidenteAsync(incidenteOpenData);
                    guardados++;
                    
                    if (guardados % 10 == 0)
                        Console.WriteLine($"✅ Guardados {guardados}/{atestados.Count()} incidentes");
                }
                catch (Exception exAtestado)
                {
                    Console.WriteLine($"❌ Error procesando atestado {atestado.Id}: {exAtestado.Message}");
                }
            }
            
            Console.WriteLine($"✅ Regeneración de atestados completada: {guardados}/{atestados.Count()} incidentes guardados");
            
            // 🔥 AGREGAR: Regenerar también alertas vencidas/no_resueltas del periodo
            try
            {
                Console.WriteLine($"🔄 Buscando alertas vencidas/no_resueltas para {anio}-{mes:D2}...");
                
                // Buscar alertas en estados finales (vencida, no_resuelta)
                var alertasNoResueltas = await _alertaRepository.ObtenerAlertasPorEstadosYRangoAsync(
                    new[] { "vencida", "no_resuelta" },
                    fechaInicio,
                    fechaFin
                );
                
                Console.WriteLine($"📋 Encontradas {alertasNoResueltas.Count()} alertas vencidas/no_resueltas");
                
                int guardadosNoResueltos = 0;
                foreach (var alerta in alertasNoResueltas)
                {
                    try
                    {
                        // Verificar si ya existe en OpenData
                        var queryExiste = _firestoreDb.Collection(OPEN_DATA_COLLECTION)
                            .WhereEqualTo("fechaCreacionAlerta", Timestamp.FromDateTime(alerta.FechaCreacion.ToUniversalTime()))
                            .WhereEqualTo("latitudRedondeada", Math.Round(alerta.Lat, 3))
                            .WhereEqualTo("longitudRedondeada", Math.Round(alerta.Lon, 3))
                            .Limit(1);
                        
                        var snapExiste = await queryExiste.GetSnapshotAsync();
                        
                        if (snapExiste.Documents.Count == 0)
                        {
                            // No existe, crear registro OpenData
                            var incidenteOpenData = alerta.ConvertirAOpenDataSinAtestado();
                            await GuardarIncidenteAsync(incidenteOpenData);
                            guardadosNoResueltos++;
                            
                            Console.WriteLine($"✅ Alerta {alerta.Id} ({alerta.Estado}) agregada a OpenData");
                        }
                        else
                        {
                            Console.WriteLine($"⏭️ Alerta {alerta.Id} ya existe en OpenData, omitiendo");
                        }
                    }
                    catch (Exception exAlerta)
                    {
                        Console.WriteLine($"❌ Error procesando alerta no resuelta {alerta.Id}: {exAlerta.Message}");
                    }
                }
                
                Console.WriteLine($"✅ Regeneración de alertas no resueltas completada: {guardadosNoResueltos} nuevas agregadas");
            }
            catch (Exception exNoResueltas)
            {
                Console.WriteLine($"⚠️ Error regenerando alertas no resueltas: {exNoResueltas.Message}");
                // No lanzar excepción, solo loggear
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en RegenerarAgregadosAsync: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
            throw;
        }
    }

    private OpenDataIncidente? MapearIncidenteDesdeFirestore(DocumentSnapshot doc)
    {
        if (!doc.Exists)
            return null;

        var data = doc.ToDictionary();

        return new OpenDataIncidente(
            id: doc.Id,
            fechaCreacionAlerta: data.ContainsKey("fechaCreacionAlerta") && data["fechaCreacionAlerta"] is Timestamp tsCreacion 
                ? tsCreacion.ToDateTime() : null,
            fechaTomadaAlerta: data.ContainsKey("fechaTomadaAlerta") && data["fechaTomadaAlerta"] is Timestamp tsTomada 
                ? tsTomada.ToDateTime() : null,
            fechaLlegada: data.ContainsKey("fechaLlegada") && data["fechaLlegada"] is Timestamp tsLlegada 
                ? tsLlegada.ToDateTime() : null,
            fechaAtendida: data.ContainsKey("fechaAtendida") && data["fechaAtendida"] is Timestamp tsAtendida 
                ? tsAtendida.ToDateTime() : null,
            fechaResuelto: data.ContainsKey("fechaResuelto") && data["fechaResuelto"] is Timestamp tsResuelto 
                ? tsResuelto.ToDateTime() : null,
            tiempoRespuestaMinutos: data.ContainsKey("tiempoRespuestaMinutos") && data["tiempoRespuestaMinutos"] != null 
                ? Convert.ToDouble(data["tiempoRespuestaMinutos"]) : null,
            tiempoLlegadaMinutos: data.ContainsKey("tiempoLlegadaMinutos") && data["tiempoLlegadaMinutos"] != null 
                ? Convert.ToDouble(data["tiempoLlegadaMinutos"]) : null,
            tiempoAtencionMinutos: data.ContainsKey("tiempoAtencionMinutos") && data["tiempoAtencionMinutos"] != null 
                ? Convert.ToDouble(data["tiempoAtencionMinutos"]) : null,
            tiempoTotalMinutos: data.ContainsKey("tiempoTotalMinutos") && data["tiempoTotalMinutos"] != null 
                ? Convert.ToDouble(data["tiempoTotalMinutos"]) : null,
            fechaIncidente: data.ContainsKey("fechaIncidente") && data["fechaIncidente"] is Timestamp ts 
                ? ts.ToDateTime() : DateTime.UtcNow,
            cantidadActivaciones: data.ContainsKey("cantidadActivaciones") ? Convert.ToInt32(data["cantidadActivaciones"]) : 0,
            esRecurrente: data.ContainsKey("esRecurrente") && data["esRecurrente"] is bool recurrente && recurrente,
            estadoFinal: data.ContainsKey("estadoFinal") ? data["estadoFinal"]?.ToString() ?? "resuelto" : "resuelto",
            nivelUrgencia: data.ContainsKey("nivelUrgencia") ? data["nivelUrgencia"]?.ToString() ?? "baja" : "baja",
            latitudRedondeada: data.ContainsKey("latitudRedondeada") ? Convert.ToDouble(data["latitudRedondeada"]) : 0,
            longitudRedondeada: data.ContainsKey("longitudRedondeada") ? Convert.ToDouble(data["longitudRedondeada"]) : 0,
            distrito: data.ContainsKey("distrito") ? data["distrito"]?.ToString() ?? "" : "",
            tipoViolencia: data.ContainsKey("tipoViolencia") ? data["tipoViolencia"]?.ToString() ?? "" : "",
            nivelRiesgo: data.ContainsKey("nivelRiesgo") ? data["nivelRiesgo"]?.ToString() ?? "" : "",
            alertaVeridica: data.ContainsKey("alertaVeridica") && data["alertaVeridica"] is bool veridica && veridica,
            edadVictimaRango: data.ContainsKey("edadVictimaRango") ? data["edadVictimaRango"]?.ToString() ?? "" : "",
            generoVictima: data.ContainsKey("generoVictima") ? data["generoVictima"]?.ToString() ?? "" : "",
            requirioAmbulancia: data.ContainsKey("requirioAmbulancia") && data["requirioAmbulancia"] is bool ambulancia && ambulancia,
            requirioRefuerzo: data.ContainsKey("requirioRefuerzo") && data["requirioRefuerzo"] is bool refuerzo && refuerzo,
            victimaTrasladadaComisaria: data.ContainsKey("victimaTrasladadaComisaria") && data["victimaTrasladadaComisaria"] is bool traslado && traslado,
            bateriaNivel: data.ContainsKey("bateriaNivel") && data["bateriaNivel"] != null ? Convert.ToInt32(data["bateriaNivel"]) : null,
            dispositivoTipo: data.ContainsKey("dispositivoTipo") ? data["dispositivoTipo"]?.ToString() ?? "boton_panico" : "boton_panico",
            fechaRegistroOpenData: data.ContainsKey("fechaRegistroOpenData") && data["fechaRegistroOpenData"] is Timestamp tsRegistro 
                ? tsRegistro.ToDateTime() : DateTime.UtcNow
        );
    }
}
