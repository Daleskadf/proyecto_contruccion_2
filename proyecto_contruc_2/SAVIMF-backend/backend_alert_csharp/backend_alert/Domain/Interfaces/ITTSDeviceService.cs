using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPI.Models; // <-- ESTE USING

namespace Domain.Interfaces
{
    public interface ITTSDeviceService
    {
        Task RegistrarDispositivoAsync(string deviceId, string devEui, string joinEui, string appKey);
        //Task<List<DispositivoTTSDto>> ListarDispositivosAsync();
    }
}