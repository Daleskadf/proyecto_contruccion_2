using Microsoft.AspNetCore.Mvc;
using Application.UseCases;
using WebAPI.Models;
using WebAPI.Filters;

[ApiController]
[Route("api/[controller]")]
public class DeviceController : ControllerBase
{
    private readonly RegistrarDispositivoTTSUseCase _registrarDispositivoTTSUseCase;
    private readonly ListarDispositivosConVinculoUseCase _listarDispositivosConVinculoUseCase;
    //private readonly ListarDispositivosTTSUseCase _listarDispositivosTTSUseCase;

    public DeviceController(
        RegistrarDispositivoTTSUseCase registrarDispositivoTTSUseCase,
        ListarDispositivosConVinculoUseCase listarDispositivosConVinculoUseCase)
    //ListarDispositivosTTSUseCase listarDispositivosTTSUseCase)
    {
        _registrarDispositivoTTSUseCase = registrarDispositivoTTSUseCase;
        _listarDispositivosConVinculoUseCase = listarDispositivosConVinculoUseCase;
        //_listarDispositivosTTSUseCase = listarDispositivosTTSUseCase;
    }

    [FirebaseAuthGuardAttribute]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarDispositivoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId) ||
            string.IsNullOrWhiteSpace(dto.DevEui) ||
            string.IsNullOrWhiteSpace(dto.JoinEui) ||
            string.IsNullOrWhiteSpace(dto.AppKey))
        {
            return BadRequest(new { mensaje = "Faltan datos obligatorios" });
        }

        try
        {
            await _registrarDispositivoTTSUseCase.EjecutarAsync(
                dto.DeviceId, dto.DevEui, dto.JoinEui, dto.AppKey
            );
            return Ok(new { mensaje = "Dispositivo registrado en The Things Stack correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
    }

    [FirebaseAuthGuardAttribute]
    [HttpGet("listar")]
    public async Task<IActionResult> Listar()
    {
        var dispositivos = await _listarDispositivosConVinculoUseCase.EjecutarAsync();

        // Log para ver qué se envía al frontend
        Console.WriteLine("=== Dispositivos enviados al frontend ===");
        foreach (var d in dispositivos)
        {
            Console.WriteLine($"DeviceId: {d.DeviceId}, DevEui: {d.DevEui}, Vinculado: {d.Vinculado}");
        }
        Console.WriteLine("========================================");

        return Ok(dispositivos);
    }
}