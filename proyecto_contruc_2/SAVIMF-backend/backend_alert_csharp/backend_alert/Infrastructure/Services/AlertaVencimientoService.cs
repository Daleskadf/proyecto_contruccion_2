using Domain.Interfaces;

namespace Infrastructure.Services
{
    public class AlertaVencimientoService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertaVencimientoService> _logger;

        public AlertaVencimientoService(IServiceProvider serviceProvider, ILogger<AlertaVencimientoService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var alertaRepository = scope.ServiceProvider.GetRequiredService<IAlertaRepository>();

                    // 🔥 MARCAR COMO VENCIDAS TODAS LAS ALERTAS QUE CUMPLIERON 10+ MINUTOS
                    await MarcarAlertasVencidas(alertaRepository);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en servicio de vencimiento de alertas");
                }

                // Ejecutar cada 2 minutos
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }

        private async Task MarcarAlertasVencidas(IAlertaRepository alertaRepository)
        {
            try
            {
                var alertas = await alertaRepository.ListarAlertasAsync();
                var ahora = DateTime.UtcNow;

                foreach (var alerta in alertas)
                {
                    // Si la alerta está disponible/tomada/llegada Y pasaron 10+ minutos desde la última activación
                    if ((alerta.Estado == "disponible" || alerta.Estado == "tomada" || alerta.Estado == "llegada") &&
                        ahora > alerta.UltimaActivacion.AddMinutes(10))
                    {
                        var updates = new Dictionary<string, object>
                        {
                            { "estado", "vencida" }
                        };

                        await alertaRepository.UpdateFieldsAsync(alerta.Id, updates);
                        _logger.LogInformation($"✅ Alerta {alerta.Id} marcada como vencida (10+ min transcurridos)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marcando alertas como vencidas");
            }
        }
    }
}