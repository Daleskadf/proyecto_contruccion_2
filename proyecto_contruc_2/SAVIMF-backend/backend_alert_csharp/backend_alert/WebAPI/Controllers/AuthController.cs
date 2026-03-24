using Microsoft.AspNetCore.Mvc;
using Application.UseCases; // Importa tu caso de uso
using WebAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;

    public AuthController(LoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
    }

    [HttpPost("firebase")]
    public async Task<IActionResult> LoginWithFirebase([FromBody] LoginFirebaseRequestDto body)
    {
        // 1. Validar token recibido
        string? token = body?.token;
        if (string.IsNullOrEmpty(token))
            return BadRequest(new { mensaje = "Token requerido" });

        try
        {
            // 2. Ejecutar caso de uso y manejar posibles errores internos
            var usuario = await _loginUseCase.EjecutarAsync(token);
            if (usuario == null)
                return Unauthorized(new { mensaje = "Usuario sin rol asignado" });

            // 3. Respuesta exitosa
            return Ok(new
            {
                token = token,
                user = new
                {
                    uid = usuario.Uid,
                    email = usuario.Email,
                    role = usuario.Role
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex); // O usa tu sistema de logs
            return StatusCode(500, new { mensaje = "Error interno, intente más tarde." });
        }
    }
}