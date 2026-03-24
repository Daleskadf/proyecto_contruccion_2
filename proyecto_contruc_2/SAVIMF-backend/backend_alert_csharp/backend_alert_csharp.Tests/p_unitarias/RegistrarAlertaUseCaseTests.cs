using NUnit.Framework;
using Moq;
using Application.UseCases;
using Domain.Entities;
using Domain.Interfaces;
using System.Threading.Tasks;

[TestFixture]
public class RegistrarAlertaUseCaseTests
{
    [Test]
    public async Task EjecutarAsync_CallsSaveAsyncOnRepository()
    {
        var repoMock = new Mock<IAlertaRepository>();
        var useCase = new RegistrarAlertaUseCase(repoMock.Object);
        var alerta = new Alerta(
            "devEUI",
            10.6857,
            20.3545,
            90,
            DateTime.UtcNow,
            "device123",
            "Usuario Test"
        );

        await useCase.EjecutarAsync(alerta);

        repoMock.Verify(r => r.SaveAsync(It.Is<Alerta>(a =>
            a.DevEUI == "devEUI" && a.Lat == 10.6857 && a.Lon == 20.3545 && a.Bateria == 90)), Times.Once);
    }
}