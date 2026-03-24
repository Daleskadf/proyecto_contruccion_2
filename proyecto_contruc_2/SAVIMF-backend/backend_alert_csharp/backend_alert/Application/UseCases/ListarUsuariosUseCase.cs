using Domain.Entities;
using Domain.Interfaces;

namespace Application.UseCases
{
    public class ListarUsuariosUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;

        public ListarUsuariosUseCase(IUserRepositoryFirestore userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<Usuario>> EjecutarAsync(int limite = 50, string? filtroRole = null)
        {
            try
            {
                // Validar límite
                if (limite <= 0 || limite > 100)
                {
                    limite = 50; // Límite por defecto
                }

                // Validar filtro de role si se proporciona
                if (!string.IsNullOrEmpty(filtroRole))
                {
                    var rolesValidos = new[] { "patrullero", "operador", "victima" };
                    if (!Array.Exists(rolesValidos, role => role.Equals(filtroRole, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new ArgumentException($"Role '{filtroRole}' no es válido");
                    }
                }

                var usuarios = await _userRepository.ListarUsuariosAsync(limite, filtroRole);

                Console.WriteLine($"[ListarUsuariosUseCase] Listados {usuarios.Count} usuarios");
                return usuarios;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ListarUsuariosUseCase] Error: {ex.Message}");
                throw;
            }
        }
    }
}