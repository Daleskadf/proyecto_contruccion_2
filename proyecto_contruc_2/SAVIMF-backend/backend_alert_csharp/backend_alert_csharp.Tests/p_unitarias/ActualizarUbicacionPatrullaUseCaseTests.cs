using NUnit.Framework;
using Moq;
using Application.UseCases;
using Domain.Entities;
using Domain.Interfaces;
using System.Threading.Tasks;

[TestFixture]
public class ActualizarUbicacionPatrullaUseCaseTests
{
    [Test]
    public async Task EjecutarAsync_CallsSaveAsyncOnRepository()
    {
        var repoMock = new Mock<IPatrulleroRepository>();
        var useCase = new ActualizarUbicacionPatrullaUseCase(repoMock.Object);

        await useCase.EjecutarAsync("patrullero1", 10.5676, 20.2389);

        repoMock.Verify(r => r.SaveAsync(It.Is<Patrulla>(p =>
            p.PatrulleroId == "patrullero1" && p.Lat == 10.5676 && p.Lon == 20.2389)), Times.Once);
    }
}
