using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models;

namespace Application.UseCases
{
    public class ListarDispositivosConVinculoUseCase
    {
        private readonly IDispositivoRepository _dispositivoRepository;
        private readonly IUserRepositoryFirestore _userRepository;

        public ListarDispositivosConVinculoUseCase(
            IDispositivoRepository dispositivoRepository,
            IUserRepositoryFirestore userRepository)
        {
            _dispositivoRepository = dispositivoRepository;
            _userRepository = userRepository;
        }

        public async Task<List<DispositivoListadoDto>> EjecutarAsync()
        {
            var dispositivos = await _dispositivoRepository.ListarAsync();
            var usuarios = await _userRepository.BuscarPorRolAsync("victima");

            var result = new List<DispositivoListadoDto>();

            foreach (var dispositivo in dispositivos)
            {
                // Busca si algún usuario tiene ese device_id
                bool vinculado = usuarios.Exists(u => u.DeviceId == dispositivo.DeviceId);

                result.Add(new DispositivoListadoDto
                {
                    DeviceId = dispositivo.DeviceId,
                    DevEui = dispositivo.DevEui,
                    Vinculado = vinculado ? "SI" : "NO"
                });
            }

            return result;
        }
    }
}