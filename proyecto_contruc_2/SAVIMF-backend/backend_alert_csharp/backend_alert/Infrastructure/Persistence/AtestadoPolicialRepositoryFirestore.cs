using Google.Cloud.Firestore;
using backend_alert.Domain.Entities;
using backend_alert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace backend_alert.Infrastructure.Persistence;

public class AtestadoPolicialRepositoryFirestore : IAtestadoPolicialRepository
{
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<AtestadoPolicialRepositoryFirestore> _logger;
    private const string COLLECTION_NAME = "atestados_policiales";

    public AtestadoPolicialRepositoryFirestore(
        FirestoreDb firestoreDb,
        ILogger<AtestadoPolicialRepositoryFirestore> logger)
    {
        _firestoreDb = firestoreDb;
        _logger = logger;
    }

    public async Task<string> GuardarAsync(AtestadoPolicial atestado)
    {
        var document = await _firestoreDb.Collection(COLLECTION_NAME).AddAsync(new
        {
            alertaId = atestado.AlertaId,
            patrulleroUid = atestado.PatrulleroUid,
            patrulleroNombre = atestado.PatrulleroNombre,
            fechaCreacion = atestado.FechaCreacion,
            fechaIncidente = atestado.FechaIncidente,

            latitud = atestado.Latitud,
            longitud = atestado.Longitud,
            distrito = atestado.Distrito,
            direccionReferencial = atestado.DireccionReferencial,

            tipoViolencia = atestado.TipoViolencia,
            nivelRiesgo = atestado.NivelRiesgo,
            alertaVeridica = atestado.AlertaVeridica,
            descripcionHechos = atestado.DescripcionHechos,

            nombreVictima = atestado.NombreVictima,
            dniVictima = atestado.DniVictima,
            edadAproximada = atestado.EdadAproximada,

            requirioAmbulancia = atestado.RequirioAmbulancia,
            requirioRefuerzo = atestado.RequirioRefuerzo,
            victimaTrasladadaComisaria = atestado.VictimaTrasladadaComisaria,
            accionesRealizadas = atestado.AccionesRealizadas,

            observaciones = atestado.Observaciones
        });

        return document.Id;
    }

    public async Task<AtestadoPolicial?> ObtenerPorIdAsync(string id)
    {
        var snap = await _firestoreDb
            .Collection(COLLECTION_NAME)
            .Document(id)
            .GetSnapshotAsync();

        return snap.Exists ? MapearDesdeFirestore(snap) : null;
    }

    public async Task<IEnumerable<AtestadoPolicial>> ObtenerTodosAsync()
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME).GetSnapshotAsync();
        return snapshot.Documents.Select(MapearDesdeFirestore);
    }

    public async Task CrearAsync(AtestadoPolicial atestado)
    {
        await _firestoreDb.Collection(COLLECTION_NAME).AddAsync(new
        {
            alertaId = atestado.AlertaId,
            patrulleroUid = atestado.PatrulleroUid,
            patrulleroNombre = atestado.PatrulleroNombre,
            fechaCreacion = atestado.FechaCreacion,
            fechaIncidente = atestado.FechaIncidente,
            latitud = atestado.Latitud,
            longitud = atestado.Longitud,
            distrito = atestado.Distrito,
            direccionReferencial = atestado.DireccionReferencial,
            tipoViolencia = atestado.TipoViolencia,
            nivelRiesgo = atestado.NivelRiesgo,
            alertaVeridica = atestado.AlertaVeridica,
            descripcionHechos = atestado.DescripcionHechos,
            nombreVictima = atestado.NombreVictima,
            dniVictima = atestado.DniVictima,
            edadAproximada = atestado.EdadAproximada,
            requirioAmbulancia = atestado.RequirioAmbulancia,
            requirioRefuerzo = atestado.RequirioRefuerzo,
            victimaTrasladadaComisaria = atestado.VictimaTrasladadaComisaria,
            accionesRealizadas = atestado.AccionesRealizadas,
            observaciones = atestado.Observaciones
        });
    }

    public async Task ActualizarAsync(AtestadoPolicial atestado)
    {
        var docRef = _firestoreDb.Collection(COLLECTION_NAME).Document(atestado.Id);
        await docRef.SetAsync(new
        {
            alertaId = atestado.AlertaId,
            patrulleroUid = atestado.PatrulleroUid,
            patrulleroNombre = atestado.PatrulleroNombre,
            fechaCreacion = atestado.FechaCreacion,
            fechaIncidente = atestado.FechaIncidente,
            latitud = atestado.Latitud,
            longitud = atestado.Longitud,
            distrito = atestado.Distrito,
            direccionReferencial = atestado.DireccionReferencial,
            tipoViolencia = atestado.TipoViolencia,
            nivelRiesgo = atestado.NivelRiesgo,
            alertaVeridica = atestado.AlertaVeridica,
            descripcionHechos = atestado.DescripcionHechos,
            nombreVictima = atestado.NombreVictima,
            dniVictima = atestado.DniVictima,
            edadAproximada = atestado.EdadAproximada,
            requirioAmbulancia = atestado.RequirioAmbulancia,
            requirioRefuerzo = atestado.RequirioRefuerzo,
            victimaTrasladadaComisaria = atestado.VictimaTrasladadaComisaria,
            accionesRealizadas = atestado.AccionesRealizadas,
            observaciones = atestado.Observaciones
        });
    }

    public async Task EliminarAsync(string id)
    {
        var docRef = _firestoreDb.Collection(COLLECTION_NAME).Document(id);
        await docRef.DeleteAsync();
    }

    public async Task<bool> ExisteParaAlertaAsync(string alertaId)
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME)
            .WhereEqualTo("alertaId", alertaId)
            .Limit(1)
            .GetSnapshotAsync();

        return snapshot.Documents.Any();
    }

    public async Task RegenerarOpenDataDelMes(int year, int month)
    {
        // Implementación específica para regenerar datos abiertos
        _logger.LogInformation("Regenerando Open Data para {Year}-{Month}", year, month);
        // Lógica de regeneración aquí
    }

    public async Task<AtestadoPolicial?> ObtenerPorAlertaIdAsync(string alertaId)
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME)
            .WhereEqualTo("alertaId", alertaId)
            .Limit(1)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(MapearDesdeFirestore).FirstOrDefault();
    }

    public async Task<IEnumerable<AtestadoPolicial>> ObtenerPorPatrulleroAsync(string patrulleroId)
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME)
            .WhereEqualTo("patrulleroUid", patrulleroId)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(MapearDesdeFirestore);
    }

    public async Task<IEnumerable<AtestadoPolicial>> ObtenerPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin)
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME)
            .WhereGreaterThanOrEqualTo("fechaIncidente", Timestamp.FromDateTime(fechaInicio.ToUniversalTime()))
            .WhereLessThanOrEqualTo("fechaIncidente", Timestamp.FromDateTime(fechaFin.ToUniversalTime()))
            .GetSnapshotAsync();

        return snapshot.Documents.Select(MapearDesdeFirestore);
    }

    public async Task<IEnumerable<AtestadoPolicial>> ObtenerPorDistritoAsync(string distrito)
    {
        var snapshot = await _firestoreDb.Collection(COLLECTION_NAME)
            .WhereEqualTo("distrito", distrito)
            .GetSnapshotAsync();

        return snapshot.Documents.Select(MapearDesdeFirestore);
    }

    private AtestadoPolicial MapearDesdeFirestore(DocumentSnapshot doc)
    {
        var d = doc.ToDictionary();

        return new AtestadoPolicial(
            id: doc.Id,
            alertaId: d["alertaId"]?.ToString() ?? "",
            patrulleroUid: d["patrulleroUid"]?.ToString() ?? "",
            patrulleroNombre: d["patrulleroNombre"]?.ToString() ?? "",
            fechaCreacion: (Timestamp)d["fechaCreacion"],
            fechaIncidente: (Timestamp)d["fechaIncidente"],
            latitud: Convert.ToDouble(d["latitud"]),
            longitud: Convert.ToDouble(d["longitud"]),
            distrito: d["distrito"]?.ToString() ?? "",
            direccionReferencial: d["direccionReferencial"]?.ToString() ?? "",
            tipoViolencia: d["tipoViolencia"]?.ToString() ?? "",
            nivelRiesgo: d["nivelRiesgo"]?.ToString() ?? "",
            alertaVeridica: Convert.ToBoolean(d["alertaVeridica"]),
            descripcionHechos: d["descripcionHechos"]?.ToString() ?? "",
            nombreVictima: d["nombreVictima"]?.ToString() ?? "",
            dniVictima: d["dniVictima"]?.ToString() ?? "",
            edadAproximada: Convert.ToInt32(d["edadAproximada"]),
            requirioAmbulancia: Convert.ToBoolean(d["requirioAmbulancia"]),
            requirioRefuerzo: Convert.ToBoolean(d["requirioRefuerzo"]),
            victimaTrasladadaComisaria: Convert.ToBoolean(d["victimaTrasladadaComisaria"]),
            accionesRealizadas: d["accionesRealizadas"]?.ToString() ?? "",
            observaciones: d["observaciones"]?.ToString() ?? ""
        );
    }
}
