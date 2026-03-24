using Domain.Entities;
using Domain.Interfaces;

namespace Application.UseCases
{
    public class RegistrarUsuarioUseCase
    {
        private readonly IUserRepositoryFirestore _userRepository;
        private readonly IFirebaseAuthService _firebaseAuthService;

        public RegistrarUsuarioUseCase(
            IUserRepositoryFirestore userRepository,
            IFirebaseAuthService firebaseAuthService)
        {
            _userRepository = userRepository;
            _firebaseAuthService = firebaseAuthService;
        }

        public async Task<string> EjecutarAsync(string email, string password, string dni, string nombre, string role)
        {
            Console.WriteLine($"[RegistrarUsuarioUseCase] Iniciando registro de usuario: {email}");

            // 1. Validar que no exista usuario con el mismo email
            var usuarioExistentePorEmail = await _userRepository.BuscarUsuarioPorEmailAsync(email);
            if (usuarioExistentePorEmail != null)
            {
                throw new InvalidOperationException($"Ya existe un usuario registrado con el email: {email}");
            }

            // 2. Validar que no exista usuario con el mismo DNI
            var usuarioExistentePorDni = await _userRepository.BuscarUsuarioPorDniAsync(dni);
            if (usuarioExistentePorDni != null)
            {
                throw new InvalidOperationException($"Ya existe un usuario registrado con el DNI: {dni}");
            }

            // 3. Validar que el role sea válido
            if (role != "patrullero" && role != "operador")
            {
                throw new ArgumentException("El role debe ser 'patrullero' u 'operador'");
            }

            try
            {
                // 4. Crear usuario en Firebase Auth
                Console.WriteLine($"[RegistrarUsuarioUseCase] Creando usuario en Firebase Auth...");
                string uid = await _firebaseAuthService.CrearUsuarioAsync(email, password);
                Console.WriteLine($"[RegistrarUsuarioUseCase] Usuario creado en Firebase Auth con UID: {uid}");

                // 5. Crear objeto Usuario con los datos adicionales
                var usuario = new Usuario(uid, email, dni, nombre, role);

                // 6. Guardar datos adicionales en Firestore
                Console.WriteLine($"[RegistrarUsuarioUseCase] Guardando datos adicionales en Firestore...");
                await _userRepository.RegistrarUsuarioAsync(usuario);

                Console.WriteLine($"[RegistrarUsuarioUseCase] Usuario registrado exitosamente. UID: {uid}");
                return uid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegistrarUsuarioUseCase] Error durante el registro: {ex.Message}");
                throw new InvalidOperationException($"Error al registrar usuario: {ex.Message}", ex);
            }
        }
    }
}