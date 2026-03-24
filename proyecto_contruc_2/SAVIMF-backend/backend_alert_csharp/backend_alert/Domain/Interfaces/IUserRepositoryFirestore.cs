using System.Threading.Tasks;
using System.Collections.Generic;
using WebAPI.Models;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUserRepositoryFirestore
    {
        Task<UsuarioDto?> BuscarPorDniAsync(string dni);
        Task<UsuarioDto?> BuscarPorUidAsync(string uid);
        Task<List<UsuarioDto>> BuscarPorRolAsync(string rol); // e.g. "victima"
        Task<string?> GetRoleByUidAsync(string uid);
        Task VincularDispositivoAsync(VincularDispositivoDto vincularDto);
        Task<UsuarioDto?> BuscarPorDeviceIdAsync(string deviceId);

        // 🆕 Métodos para registro de patrulleros/operadores
        Task<string> RegistrarUsuarioAsync(Usuario usuario);
        Task<Usuario?> BuscarUsuarioPorEmailAsync(string email);
        Task<Usuario?> BuscarUsuarioPorDniAsync(string dni);
        Task<List<Usuario>> ListarUsuariosPorRoleAsync(string role);

        // 🆕 Métodos para listar y editar usuarios
        Task<List<Usuario>> ListarUsuariosAsync(int limite = 50, string? filtroRole = null);
        Task<bool> EditarUsuarioAsync(Usuario usuario);

        // 🆕 Métodos para FCM Push Notifications
        Task<bool> ActualizarFcmTokenAsync(string uid, string fcmToken);
        Task<List<string>> ObtenerTokensFcmPorRoleAsync(string role, bool soloActivos = true);
        Task<bool> ActualizarUltimaConexionAsync(string uid);
        Task<List<Usuario>> ObtenerUsuariosConFcmActivoAsync(string? role = null);
    }
}