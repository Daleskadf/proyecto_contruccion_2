using System.Threading.Tasks;
using System.Collections.Generic;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAlertaRepository
    {
        Task SaveAsync(Alerta alerta);
        Task<List<Alerta>> ListarAlertasAsync();
        
        // Obtener alerta por ID
        Task<Alerta?> ObtenerPorIdAsync(string alertaId);

        // Actualiza campos específicos de una alerta por su ID
        Task UpdateFieldsAsync(string alertaId, IDictionary<string, object> updates);

        Task<Alerta?> BuscarAlertaRecienteAsync(string deviceId, DateTime desde);

        // 🔥 NUEVOS MÉTODOS PARA SISTEMA DE PRIORIDADES
        Task<List<Alerta>> BuscarAlertasArchivadasAsync(string deviceId);
        Task ArchivarAlertasVencidas();
        Task<List<Alerta>> ListarAlertasActivasAsync(); // Solo alertas no archivadas

        // 🔍 MÉTODO PARA BUSCAR ALERTAS POR DEVICE_ID Y TIEMPO (vencimiento automático)
        Task<List<Alerta>> BuscarAlertasPorDeviceIdYTiempoAsync(string devEUI, DateTime fechaDesde);

        // 📊 MÉTODO PARA ANÁLISIS Y MAPA DE CALOR
        Task<List<Alerta>> ObtenerAlertasPorRangoFechas(DateTime fechaInicio, DateTime fechaFin);
        
        // 🔥 MÉTODO PARA OBTENER ALERTAS POR ESTADOS Y RANGO DE FECHAS (OpenData)
        Task<List<Alerta>> ObtenerAlertasPorEstadosYRangoAsync(string[] estados, DateTime fechaInicio, DateTime fechaFin);
    }
}