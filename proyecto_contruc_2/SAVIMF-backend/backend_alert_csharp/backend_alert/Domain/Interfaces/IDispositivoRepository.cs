using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPI.Models;

namespace Domain.Interfaces
{
    public interface IDispositivoRepository
    {
        Task GuardarAsync(DispositivoTTSDto dispositivo);
        Task<List<DispositivoTTSDto>> ListarAsync(); // <--- nuevo método
    }
}