using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models;

namespace Application.UseCases
{
    public class VincularDispositivoUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;

        public VincularDispositivoUseCase(IUserRepositoryFirestore userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<string> EjecutarAsync(VincularDispositivoDto dto)
        {
            var usuario = await _userRepository.BuscarPorDniAsync(dto.Dni);
            if (usuario == null)
                throw new Exception("Usuario no encontrado.");

            await _userRepository.VincularDispositivoAsync(dto);
            return "Dispositivo vinculado correctamente.";
        }
    }
}