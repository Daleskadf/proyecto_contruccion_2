using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using backend_alert.Domain.Entities;

namespace backend_alert.Domain.Interfaces
{
    /// <summary>
    /// Interfaz para el repositorio de Atestados Policiales.
    /// Define las operaciones básicas para interactuar con la base de datos.
    /// </summary>
    public interface IAtestadoPolicialRepository
    {
        Task<IEnumerable<AtestadoPolicial>> ObtenerTodosAsync();
        Task<AtestadoPolicial?> ObtenerPorIdAsync(string id);
        Task CrearAsync(AtestadoPolicial atestado);
        Task ActualizarAsync(AtestadoPolicial atestado);
        Task EliminarAsync(string id);
        Task<bool> ExisteParaAlertaAsync(string alertaId);
    Task<string> GuardarAsync(AtestadoPolicial atestado);
        Task RegenerarOpenDataDelMes(int year, int month);
        Task<AtestadoPolicial?> ObtenerPorAlertaIdAsync(string alertaId);
        Task<IEnumerable<AtestadoPolicial>> ObtenerPorPatrulleroAsync(string patrulleroId);
        Task<IEnumerable<AtestadoPolicial>> ObtenerPorRangoFechasAsync(DateTime fechaInicio, DateTime fechaFin);
        Task<IEnumerable<AtestadoPolicial>> ObtenerPorDistritoAsync(string distrito);
    }
}