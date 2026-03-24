using Domain.Entities;
using Domain.Interfaces;
namespace Application.UseCases
{
    public class ActualizarUbicacionPatrullaUseCase
    {
        private readonly IPatrulleroRepository _patrulleroRepository;

        public ActualizarUbicacionPatrullaUseCase(IPatrulleroRepository patrulleroRepository)
        {
            _patrulleroRepository = patrulleroRepository;
        }

        public async Task EjecutarAsync(string patrulleroId, double lat, double lon)
        {
            var patrulla = new Patrulla(patrulleroId, lat, lon, DateTime.UtcNow);
            await _patrulleroRepository.SaveAsync(patrulla);
        }
    }
}