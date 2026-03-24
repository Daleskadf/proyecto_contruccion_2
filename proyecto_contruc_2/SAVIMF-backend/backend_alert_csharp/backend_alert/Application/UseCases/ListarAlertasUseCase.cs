using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.UseCases
{
    public class ListarAlertasUseCase
    {
        private readonly IAlertaRepository _alertaRepository;

        public ListarAlertasUseCase(IAlertaRepository alertaRepository)
        {
            _alertaRepository = alertaRepository;
        }

        public async Task<List<Alerta>> EjecutarAsync()
        {
            return await _alertaRepository.ListarAlertasAsync();
        }
    }
}