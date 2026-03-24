/* using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models;

namespace Application.UseCases
{
    public class ListarDispositivosTTSUseCase
    {
        private readonly ITTSDeviceService _ttsDeviceService;

        public ListarDispositivosTTSUseCase(ITTSDeviceService ttsDeviceService)
        {
            _ttsDeviceService = ttsDeviceService;
        }

        public Task<List<DispositivoTTSDto>> EjecutarAsync()
            => _ttsDeviceService.ListarDispositivosAsync();
    }
} */