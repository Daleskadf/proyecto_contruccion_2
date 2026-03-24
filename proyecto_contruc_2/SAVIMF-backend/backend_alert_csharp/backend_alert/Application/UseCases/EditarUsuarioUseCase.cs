using Domain.Entities;
using Domain.Interfaces;

namespace Application.UseCases
{
    public class EditarUsuarioUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;

        public EditarUsuarioUseCase(IUserRepositoryFirestore userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> EjecutarAsync(string uid, string email, string dni, string nombre, string role, string estado, bool emailVerified)
        {
            try
            {
                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(uid))
                    throw new ArgumentException("UID es requerido");

                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("Email es requerido");

                if (string.IsNullOrWhiteSpace(dni))
                    throw new ArgumentException("DNI es requerido");

                if (string.IsNullOrWhiteSpace(nombre))
                    throw new ArgumentException("Nombre es requerido");

                // Validar que el usuario existe
                var usuarioExistente = await _userRepository.BuscarUsuarioPorEmailAsync(email);
                if (usuarioExistente == null || usuarioExistente.Uid != uid)
                {
                    // Verificar por UID también
                    var usuarioPorUid = await _userRepository.BuscarPorUidAsync(uid);
                    if (usuarioPorUid == null)
                    {
                        throw new ArgumentException("Usuario no encontrado");
                    }
                }

                // Validar role
                var rolesValidos = new[] { "patrullero", "operador", "victima" };
                if (!Array.Exists(rolesValidos, r => r.Equals(role, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"Role '{role}' no es válido");
                }

                // Validar estado
                var estadosValidos = new[] { "activo", "inactivo", "suspendido" };
                if (!Array.Exists(estadosValidos, e => e.Equals(estado, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"Estado '{estado}' no es válido");
                }

                // Crear objeto Usuario con los nuevos datos
                var usuarioActualizado = new Usuario
                {
                    Uid = uid,
                    Email = email,
                    Dni = dni,
                    Nombre = nombre,
                    Role = role.ToLower(),
                    Estado = estado.ToLower(),
                    EmailVerified = emailVerified,
                    FechaRegistro = usuarioExistente?.FechaRegistro ?? DateTime.UtcNow
                };

                var resultado = await _userRepository.EditarUsuarioAsync(usuarioActualizado);

                Console.WriteLine($"[EditarUsuarioUseCase] Usuario {uid} actualizado: {resultado}");
                return resultado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EditarUsuarioUseCase] Error: {ex.Message}");
                throw;
            }
        }
    }
}