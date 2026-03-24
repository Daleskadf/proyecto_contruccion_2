using Domain.Interfaces;
using backend_alert.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.BackgroundServices
{
    /// <summary>
    /// 🕐 Servicio en segundo plano que revisa cada minuto alertas que han cumplido >10 minutos
    /// sin ser atendidas y las marca como vencidas o no_resueltas automáticamente
    /// </summary>
    public class AlertaExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertaExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Revisar cada 1 minuto

        public AlertaExpirationService(
            IServiceProvider serviceProvider,
            ILogger<AlertaExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🕐 AlertaExpirationService iniciado - Revisando alertas cada {Interval} minutos", _checkInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MarcarAlertasExpiradas();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en AlertaExpirationService");
                }

                // Esperar 1 minuto antes de la siguiente revisión
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task MarcarAlertasExpiradas()
        {
            using var scope = _serviceProvider.CreateScope();
            var alertaRepository = scope.ServiceProvider.GetRequiredService<IAlertaRepository>();
            var openDataRepository = scope.ServiceProvider.GetRequiredService<IOpenDataRepository>();

            var ahora = DateTime.UtcNow;
            var limite = ahora.AddMinutes(-10); // Alertas con más de 10 minutos

            _logger.LogInformation("🔍 Revisando alertas creadas antes de {Limite}", limite);

            try
            {
                // Obtener todas las alertas activas
                var todasLasAlertas = await alertaRepository.ListarAlertasAsync();
                
                var alertasExpiradas = todasLasAlertas.Where(a =>
                    // Alertas en estados activos (incluyendo ATENDIDA sin atestado)
                    (a.Estado == "disponible" || a.Estado == "tomada" || a.Estado == "llegada" || a.Estado == "atendida") &&
                    // Creadas hace más de 10 minutos
                    a.FechaCreacion < limite
                ).ToList();

                _logger.LogInformation("📋 Encontradas {Count} alertas expiradas (>10 min sin resolver)", alertasExpiradas.Count);

                foreach (var alerta in alertasExpiradas)
                {
                    // 🔥 Determinar nuevo estado según el estado actual:
                    // - TOMADA/LLEGADA/ATENDIDA → NO_RESUELTA (había patrullero asignado pero no completó proceso)
                    // - DISPONIBLE → VENCIDA (nadie la tomó)
                    string nuevoEstado = (alerta.Estado == "tomada" || alerta.Estado == "llegada" || alerta.Estado == "atendida") 
                        ? "no_resuelta" 
                        : "vencida";

                    // 📊 GENERAR OPENDATA ANTES DE MARCAR COMO EXPIRADA (con estado correcto)
                    try
                    {
                        // Actualizar temporalmente el estado para el OpenData
                        var alertaTemp = new Alerta(
                            alerta.DevEUI, alerta.Lat, alerta.Lon, alerta.Bateria, alerta.Timestamp, alerta.FechaCreacion,
                            alerta.DeviceId, alerta.NombreVictima, nuevoEstado, alerta.FechaLlegada, alerta.FechaAtendida,
                            alerta.FechaResuelto, alerta.FechaTomada, alerta.PatrulleroAsignado,
                            alerta.CantidadActivaciones, alerta.UltimaActivacion, alerta.NivelUrgencia, alerta.EsRecurrente
                        );
                        
                        var openDataIncidente = alertaTemp.ConvertirAOpenDataSinAtestado();
                        await openDataRepository.GuardarIncidenteAsync(openDataIncidente);
                        
                        _logger.LogInformation("✅ OpenData generado para alerta expirada: {AlertaId} (estado: {Estado})", alerta.Id, nuevoEstado);
                    }
                    catch (Exception exOpenData)
                    {
                        _logger.LogError(exOpenData, "❌ Error generando OpenData para alerta {AlertaId}", alerta.Id);
                    }

                    // Actualizar estado en Firestore
                    var updates = new Dictionary<string, object>
                    {
                        { "estado", nuevoEstado }
                    };

                    await alertaRepository.UpdateFieldsAsync(alerta.Id, updates);

                    _logger.LogWarning(
                        "⏰ Alerta {AlertaId} marcada como {Estado} (creada: {Creacion}, edad: {Edad}min, víctima: {Victima})",
                        alerta.Id,
                        nuevoEstado.ToUpper(),
                        alerta.FechaCreacion,
                        (ahora - alerta.FechaCreacion).TotalMinutes,
                        alerta.NombreVictima
                    );
                }

                if (alertasExpiradas.Count > 0)
                {
                    _logger.LogInformation("✅ {Count} alertas marcadas como expiradas y registradas en OpenData", alertasExpiradas.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marcando alertas expiradas");
            }
        }
    }
}
