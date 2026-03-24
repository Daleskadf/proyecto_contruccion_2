using Domain.Interfaces;
namespace Application.UseCases
{
    public class RegistrarAlertaUseCase
    {
        private readonly IAlertaRepository _alertaRepository;

        public RegistrarAlertaUseCase(IAlertaRepository alertaRepository)
        {
            _alertaRepository = alertaRepository;
        }

        public async Task EjecutarAsync(Alerta nuevaAlerta)
        {
            // 🔍 Buscar alertas ACTIVAS recientes (últimos 10 minutos, NO vencidas/no_resuelta)
            var desde = DateTime.UtcNow.AddMinutes(-10);
            Console.WriteLine($"[UseCase] BuscarAlertaRecienteAsync para {nuevaAlerta.DevEUI} desde {desde:O}");
            var alertaReciente = await _alertaRepository.BuscarAlertaRecienteAsync(
                nuevaAlerta.DevEUI,
                desde
            );

            // ✅ FILTRAR: Solo considerar alertas ACTIVAS (no vencidas ni no_resuelta)
            if (alertaReciente != null && (alertaReciente.Estado == "vencida" || alertaReciente.Estado == "no_resuelta" || alertaReciente.Estado == "resuelto"))
            {
                Console.WriteLine($"[UseCase] Alerta encontrada pero está en estado '{alertaReciente.Estado}', se tratará como NO reciente");
                alertaReciente = null; // Ignorar alertas ya cerradas
            }

            if (alertaReciente != null)
            {
                Console.WriteLine($"[UseCase] ✅ Alerta ACTIVA encontrada: id={alertaReciente.Id} estado={alertaReciente.Estado} cantidadActivaciones={alertaReciente.CantidadActivaciones}");
                
                // 🔥 INCREMENTAR CONTADOR DE ACTIVACIONES (actualizamos la misma alerta)
                var nuevasCantidadActivaciones = alertaReciente.CantidadActivaciones + 1;
                var nuevoNivelUrgencia = CalcularNivelUrgencia(nuevasCantidadActivaciones);

                var updates = new Dictionary<string, object>
                {
                    { "lat", nuevaAlerta.Lat },
                    { "lon", nuevaAlerta.Lon },
                    { "bateria", nuevaAlerta.Bateria },
                    { "timestamp", DateTime.UtcNow },
                    { "cantidadActivaciones", nuevasCantidadActivaciones },
                    { "ultimaActivacion", DateTime.UtcNow },
                    { "nivelUrgencia", nuevoNivelUrgencia }
                };
                await _alertaRepository.UpdateFieldsAsync(alertaReciente.Id, updates);
                Console.WriteLine($"🔄 Alerta actualizada: {alertaReciente.Id}");
            }
            else
            {
                Console.WriteLine($"[UseCase] ❌ NO se encontró alerta ACTIVA reciente para {nuevaAlerta.DevEUI}");
                
                // 🔥 BUSCAR LA ALERTA MÁS RECIENTE DEL MISMO DISPOSITIVO (cualquier estado)
                var todasLasAlertas = await _alertaRepository.ListarAlertasAsync();
                var alertaAnterior = todasLasAlertas
                    .Where(a => a.DevEUI == nuevaAlerta.DevEUI)
                    .OrderByDescending(a => a.UltimaActivacion)
                    .FirstOrDefault();

                if (alertaAnterior != null)
                {
                    Console.WriteLine($"📋 Encontrada alerta anterior: {alertaAnterior.Id} (estado: {alertaAnterior.Estado}, activaciones: {alertaAnterior.CantidadActivaciones})");

                    // ⏰ Calcular tiempo transcurrido desde la última activación
                    var tiempoTranscurrido = DateTime.UtcNow - alertaAnterior.UltimaActivacion;
                    var horasTranscurridas = tiempoTranscurrido.TotalHours;
                    Console.WriteLine($"⏱️ Tiempo transcurrido desde última activación: {horasTranscurridas:F2} horas");

                    // ✅ CONDICIONES PARA CREAR ALERTA COMPLETAMENTE NUEVA (SIN heredar datos):
                    // 1. La alerta anterior está RESUELTA
                    // 2. Pasaron más de 5 HORAS desde la última activación (caso nuevo, no recurrencia)
                    if (alertaAnterior.Estado == "resuelto" || horasTranscurridas > 5)
                    {
                        if (alertaAnterior.Estado == "resuelto")
                        {
                            Console.WriteLine($"✅ La alerta anterior está RESUELTA, se creará una alerta completamente nueva");
                        }
                        else
                        {
                            Console.WriteLine($"✅ Pasaron más de 5 horas desde la última alerta, se creará una alerta completamente nueva (no es recurrencia)");
                        }
                        
                        nuevaAlerta.CantidadActivaciones = 1; // Reiniciar contador
                        nuevaAlerta.NivelUrgencia = "baja"; // Urgencia inicial
                        nuevaAlerta.EsRecurrente = false; // NO es recurrente
                    }
                    else
                    {
                        // 🔥 COPIAR DATOS DE LA ALERTA ANTERIOR (solo si NO está resuelta Y pasaron <5 horas)
                        Console.WriteLine($"🔴 Alerta dentro de las 5 horas, se heredarán datos (recurrencia)");
                        nuevaAlerta.CantidadActivaciones = alertaAnterior.CantidadActivaciones + 1;
                        nuevaAlerta.NivelUrgencia = "critica"; // Siempre crítica cuando pasa >10 min
                        nuevaAlerta.EsRecurrente = true;

                        // 🔥 MARCAR LA ALERTA ANTERIOR COMO VENCIDA/NO_RESUELTA (si aún no lo está)
                        if (alertaAnterior.Estado == "disponible" || alertaAnterior.Estado == "tomada" || alertaAnterior.Estado == "llegada")
                        {
                            string nuevoEstado = alertaAnterior.Estado == "tomada" ? "no_resuelta" : "vencida";
                            var updateEstado = new Dictionary<string, object> { { "estado", nuevoEstado } };
                            await _alertaRepository.UpdateFieldsAsync(alertaAnterior.Id, updateEstado);
                            Console.WriteLine($"✅ Alerta anterior {alertaAnterior.Id} marcada como {nuevoEstado.ToUpper()}");
                        }

                        Console.WriteLine($"🔴 CREANDO NUEVA ALERTA CRÍTICA: {nuevaAlerta.CantidadActivaciones} activaciones totales");
                    }
                }
                else
                {
                    Console.WriteLine($"📋 Primera alerta del dispositivo {nuevaAlerta.DevEUI}");
                }

                // ✅ SIEMPRE CREAR LA NUEVA ALERTA (no actualizar la vencida)
                await _alertaRepository.SaveAsync(nuevaAlerta);
                Console.WriteLine($"✅ Nueva alerta creada con ID generado por Firestore");
            }
        }

        // 🎯 MÉTODO PARA CALCULAR NIVEL DE URGENCIA BASADO EN ACTIVACIONES
        private string CalcularNivelUrgencia(int cantidadActivaciones)
        {
            if (cantidadActivaciones >= 4) return "critica";
            if (cantidadActivaciones >= 2) return "media";
            return "baja";
        }
    }
}