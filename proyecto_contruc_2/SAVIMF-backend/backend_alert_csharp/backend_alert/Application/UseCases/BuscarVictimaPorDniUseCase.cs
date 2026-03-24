using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models;

namespace Application.UseCases
{
    public class BuscarUsuarioPorDniUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;

        public BuscarUsuarioPorDniUseCase(IUserRepositoryFirestore userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UsuarioDto?> EjecutarAsync(string dni, string? role = null)
        {
            var usuario = await _userRepository.BuscarPorDniAsync(dni);
            if (role == null || (usuario != null && usuario.Role == role))
                return usuario;
            return null;
        }
    }
}