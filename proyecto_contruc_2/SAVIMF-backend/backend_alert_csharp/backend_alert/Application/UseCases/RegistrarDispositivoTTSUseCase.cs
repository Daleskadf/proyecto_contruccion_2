using Domain.Interfaces;
using WebAPI.Models;

namespace Application.UseCases
{
    public class RegistrarDispositivoTTSUseCase
    {
        private readonly ITTSDeviceService _ttsDeviceService;
        private readonly IDispositivoRepository _dispositivoRepository;

        public RegistrarDispositivoTTSUseCase(
            ITTSDeviceService ttsDeviceService,
            IDispositivoRepository dispositivoRepository)
        {
            _ttsDeviceService = ttsDeviceService;
            _dispositivoRepository = dispositivoRepository;
        }

        public async Task EjecutarAsync(string deviceId, string devEui, string joinEui, string appKey)
        {
            // 1. Registrar en TTS
            await _ttsDeviceService.RegistrarDispositivoAsync(deviceId, devEui, joinEui, appKey);

            // 2. Guardar en Firestore
            var dispositivo = new DispositivoTTSDto
            {
                DeviceId = deviceId,
                DevEui = devEui,
                JoinEui = joinEui,
                AppKey = appKey,
                CreatedAt = DateTime.UtcNow,    // <-- DateTime, no string
                UpdatedAt = DateTime.UtcNow
            };
            await _dispositivoRepository.GuardarAsync(dispositivo);
        }
    }
}