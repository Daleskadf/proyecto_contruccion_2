using FirebaseAdmin;
using FirebaseAdmin.Auth;
using System.Threading.Tasks;
using Domain.Interfaces;
using WebAPI.Models; // Importa tu DTO

namespace Infrastructure.Auth
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly FirebaseApp _firebaseApp;
        public FirebaseAuthService(FirebaseApp firebaseApp)
        {
            _firebaseApp = firebaseApp;
        }

        public async Task<UsuarioFirebaseDto> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                var firebaseAuth = FirebaseAuth.GetAuth(_firebaseApp);
                var decodedToken = await firebaseAuth.VerifyIdTokenAsync(idToken);
                var userRecord = await firebaseAuth.GetUserAsync(decodedToken.Uid);

                return new UsuarioFirebaseDto
                {
                    Uid = userRecord.Uid,
                    Email = userRecord.Email
                };
            }
            catch
            {
                throw new Exception("Token firebase inválido o expirado");
            }
        }

        // 🆕 Método para crear usuarios en Firebase Auth
        public async Task<string> CrearUsuarioAsync(string email, string password)
        {
            try
            {
                Console.WriteLine($"[CrearUsuarioAsync] Creando usuario en Firebase Auth: {email}");

                var firebaseAuth = FirebaseAuth.GetAuth(_firebaseApp);

                var userRecordArgs = new UserRecordArgs()
                {
                    Email = email,
                    Password = password,
                    EmailVerified = false // Se verificará por email
                };

                var userRecord = await firebaseAuth.CreateUserAsync(userRecordArgs);

                Console.WriteLine($"[CrearUsuarioAsync] Usuario creado exitosamente. UID: {userRecord.Uid}");
                return userRecord.Uid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CrearUsuarioAsync] Error al crear usuario: {ex.Message}");
                throw new InvalidOperationException($"Error al crear usuario en Firebase Auth: {ex.Message}", ex);
            }
        }
    }
}