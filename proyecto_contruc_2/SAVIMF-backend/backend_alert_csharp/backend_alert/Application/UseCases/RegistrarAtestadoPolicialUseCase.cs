using backend_alert.Domain.Entities;
using backend_alert.Domain.Interfaces;
using Domain.Interfaces;

namespace backend_alert.Application.UseCases;

/// <summary>
/// Caso de uso para registrar un atestado policial
/// Pattern: Use Case (Clean Architecture) + Command (CQRS)
/// </summary>
public class RegistrarAtestadoPolicialUseCase
{
    private readonly IAtestadoPolicialRepository _atestadoRepository;
    private readonly IOpenDataRepository _openDataRepository;
    private readonly IAlertaRepository _alertaRepository;
    private readonly ILogger<RegistrarAtestadoPolicialUseCase> _logger;

    public RegistrarAtestadoPolicialUseCase(
        IAtestadoPolicialRepository atestadoRepository,
        IOpenDataRepository openDataRepository,
        IAlertaRepository alertaRepository,
        ILogger<RegistrarAtestadoPolicialUseCase> logger)
    {
        _atestadoRepository = atestadoRepository;
        _openDataRepository = openDataRepository;
        _alertaRepository = alertaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Registra un atestado policial y genera datos para Open Data
    /// </summary>
    public async Task<string> EjecutarAsync(AtestadoPolicial atestado)
    {
        try
        {
            // 1️⃣ Validar que el atestado sea válido
            if (!atestado.EsValido())
            {
                throw new ArgumentException("El atestado no contiene los datos mínimos requeridos");
            }

            // 2️⃣ Verificar que no exista ya un atestado para esta alerta
            var existeAtestado = await _atestadoRepository.ExisteParaAlertaAsync(atestado.AlertaId);
            if (existeAtestado)
            {
                _logger.LogWarning("Ya existe un atestado para la alerta {AlertaId}", atestado.AlertaId);
                throw new InvalidOperationException($"Ya existe un atestado para la alerta {atestado.AlertaId}");
            }

            // 3️⃣ Verificar que la alerta exista (no importa el estado)
            var alerta = await _alertaRepository.ObtenerPorIdAsync(atestado.AlertaId);
            if (alerta == null)
            {
                throw new ArgumentException($"La alerta {atestado.AlertaId} no existe");
            }

            // 4️⃣ Guardar el atestado policial
            var atestadoId = await _atestadoRepository.GuardarAsync(atestado);
            _logger.LogInformation("Atestado policial {AtestadoId} registrado para alerta {AlertaId}", 
                atestadoId, atestado.AlertaId);

            // 5️⃣ Marcar la alerta como resuelta automáticamente después de registrar el atestado
            // Nota: NO escribimos campos adicionales en la colección 'alertas' para evitar cambios persistentes.
            // En su lugar, usamos fallbacks en memoria cuando generamos OpenData.
            if (alerta.Estado.ToLower() != "resuelto")
            {
                var updates = new Dictionary<string, object>
                {
                    { "estado", "resuelto" },
                    { "fechaResuelto", DateTime.UtcNow }
                };

                // Solo marcar como resuelto persistente; no añadimos fechaTomada/fechaEnCamino aquí.
                await _alertaRepository.UpdateFieldsAsync(atestado.AlertaId, updates);
                _logger.LogInformation("Alerta {AlertaId} marcada como resuelta después de registrar atestado", atestado.AlertaId);

                // Volver a leer la alerta actualizada para obtener el campo fechaResuelto y otros metadatos
                alerta = await _alertaRepository.ObtenerPorIdAsync(atestado.AlertaId);
            }

            // 6️⃣ Generar datos anónimos para Open Data (usar datos de la alerta ACTUALIZADA)
            // LOG: imprimir campos clave de la alerta re-leída para diagnóstico
            try
            {
                Console.WriteLine("--- DEBUG RegistrarAtestado: alerta usada para OpenData ---");
                Console.WriteLine($"Alerta.Id={alerta?.Id}");
                Console.WriteLine($"Alerta.FechaCreacion={alerta?.FechaCreacion:u}");
                Console.WriteLine($"Alerta.FechaTomada={(alerta?.FechaTomada.HasValue == true ? alerta?.FechaTomada.Value.ToString("u") : "NULL")}");
                Console.WriteLine($"Alerta.FechaLlegada={(alerta?.FechaLlegada.HasValue == true ? alerta?.FechaLlegada.Value.ToString("u") : "NULL")}");
                Console.WriteLine($"Alerta.FechaAtendida={(alerta?.FechaAtendida.HasValue == true ? alerta?.FechaAtendida.Value.ToString("u") : "NULL")}");
                Console.WriteLine($"Alerta.FechaResuelto={(alerta?.FechaResuelto.HasValue == true ? alerta?.FechaResuelto.Value.ToString("u") : "NULL")}");
                Console.WriteLine($"Alerta.Bateria={alerta?.Bateria} CantAct={alerta?.CantidadActivaciones} NivelUrg={alerta?.NivelUrgencia} Estado={alerta?.Estado}");
                Console.WriteLine("--- /DEBUG ---");
            }
            catch (Exception dbgEx)
            {
                Console.WriteLine($"⚠️ Error debug log RegistrarAtestado: {dbgEx.Message}");
            }

            var openDataIncidente = atestado.ConvertirAOpenData(alerta);
            await _openDataRepository.GuardarIncidenteAsync(openDataIncidente);
            _logger.LogInformation("Datos anónimos generados para Open Data del atestado {AtestadoId}", atestadoId);

            // 7️⃣ Actualizar estadísticas agregadas del distrito/mes
            await ActualizarEstadisticasAgregadasAsync(atestado);

            return atestadoId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar atestado policial para alerta {AlertaId}", atestado.AlertaId);
            throw;
        }
    }

    /// <summary>
    /// Actualiza las estadísticas agregadas del distrito/periodo
    /// Pattern: Strategy - estrategia de agregación de datos
    /// </summary>
    private async Task ActualizarEstadisticasAgregadasAsync(AtestadoPolicial atestado)
    {
        try
        {
            var anio = atestado.FechaIncidente.ToDateTime().Year;
            var mes = atestado.FechaIncidente.ToDateTime().Month;

            // Obtener o crear agregado
            var agregado = await _openDataRepository.ObtenerAgregadoAsync(atestado.Distrito, anio, mes);
            
            if (agregado == null)
            {
                agregado = new OpenDataAgregado(atestado.Distrito, anio, mes)
                {
                    LatitudCentral = atestado.Latitud,
                    LongitudCentral = atestado.Longitud
                };
            }

            // Agregar el incidente a las estadísticas (usar datos de la alerta si existe)
            var alertaParaAgregado = await _alertaRepository.ObtenerPorIdAsync(atestado.AlertaId);
            var openDataIncidente = atestado.ConvertirAOpenData(alertaParaAgregado);
            agregado.AgregarIncidente(openDataIncidente);

            // Guardar agregado actualizado
            await _openDataRepository.GuardarAgregadoAsync(agregado);
            
            _logger.LogInformation("Estadísticas agregadas actualizadas para {Distrito} {Anio}-{Mes}", 
                atestado.Distrito, anio, mes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estadísticas agregadas para distrito {Distrito}", 
                atestado.Distrito);
            // No lanzamos la excepción para no bloquear el registro del atestado
        }
    }
}
