using System.Threading.Tasks;
using Domain.Interfaces;
using Domain.Entities; // Importa la entidad Usuario
using FirebaseAdmin.Auth;

namespace Application.UseCases
{
    public class LoginUseCase
    {
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly IUserRepositoryFirestore _userRepo;

        public LoginUseCase(
            IFirebaseAuthService firebaseAuthService,
            IUserRepositoryFirestore userRepo)
        {
            _firebaseAuthService = firebaseAuthService;
            _userRepo = userRepo;
        }

        // Ahora devuelve Usuario (la entidad), no LoginResult
        public async Task<Usuario?> EjecutarAsync(string token)
        {
            var userDto = await _firebaseAuthService.VerifyIdTokenAsync(token);
            var role = await _userRepo.GetRoleByUidAsync(userDto.Uid);

            if (string.IsNullOrEmpty(role))
                return null;

            // Usar el constructor vacío y asignar propiedades directamente
            return new Usuario
            {
                Uid = userDto.Uid,
                Email = userDto.Email,
                Role = role,
                FechaRegistro = DateTime.UtcNow,
                EmailVerified = true, // Si el token es válido, asumimos que el email está verificado
                Estado = "activo"
            };
        }
    }
}