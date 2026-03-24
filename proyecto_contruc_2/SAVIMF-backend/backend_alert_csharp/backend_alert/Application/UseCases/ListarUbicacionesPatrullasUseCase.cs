using Domain.Entities;
using Domain.Interfaces;

namespace Application.UseCases
{
    public class ListarUbicacionesPatrullasUseCase
    {
        private readonly IPatrulleroRepository _patrullaRepository;

        public ListarUbicacionesPatrullasUseCase(IPatrulleroRepository patrullaRepository)
        {
            _patrullaRepository = patrullaRepository;
        }

        public async Task<List<Patrulla>> EjecutarAsync()
        {
            return await _patrullaRepository.GetUltimasPatrullasAsync();
        }
    }
}