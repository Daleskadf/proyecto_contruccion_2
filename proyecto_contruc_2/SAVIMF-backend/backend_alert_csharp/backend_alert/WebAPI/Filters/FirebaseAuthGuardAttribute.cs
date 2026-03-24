using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FirebaseAdmin.Auth;

namespace WebAPI.Filters
{
    public class FirebaseAuthGuardAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 1. Permitir preflight OPTIONS sin autenticación (CORS requirement, no es inseguro)
            if (context.HttpContext.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                // Opcional: puedes agregar headers CORS aquí si lo necesitas adicionalmente
                return;
            }

            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(decodedToken.Uid);

                // 🔥 IMPORTANTE: Guardar el UID en el contexto para que UserController lo use
                context.HttpContext.Items["FirebaseUser"] = userRecord;
                context.HttpContext.Items["FirebaseUid"] = decodedToken.Uid; // ← Esta línea era la que faltaba
                context.HttpContext.Items["FirebaseEmail"] = decodedToken.Claims.GetValueOrDefault("email");
            }
            catch
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}