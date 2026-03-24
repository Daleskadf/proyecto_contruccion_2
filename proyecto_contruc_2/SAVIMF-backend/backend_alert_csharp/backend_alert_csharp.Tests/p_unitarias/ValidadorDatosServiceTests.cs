using NUnit.Framework;
using Infrastructure.Services;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

[TestFixture]
public class ValidadorDatosServiceTests
{
    private Mock<ILogger<ValidadorDatosService>> _loggerMock;
    private ValidadorDatosService _service;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ValidadorDatosService>>();
        _service = new ValidadorDatosService(_loggerMock.Object);
    }

    [Test]
    public void ValidarDatosAlerta_AlertaInvalida_ReturnsFalse()
    {
        var alerta = new Alerta("", 0, 0, 0, DateTime.UtcNow, "device123", "Usuario Test");
        Assert.IsFalse(_service.ValidarDatosAlerta(alerta));
    }

    [Test]
    public void ValidarDatosAlerta_AlertaRealista_ReturnsTrue()
    {
        var alerta = new Alerta(
            "70B3D57ED0072E7F",
            12.3456,
            -76.5432,
            78,
            new DateTime(2025, 10, 6, 13, 49, 14, DateTimeKind.Local),
            "device123",
            "Usuario Test"
        );
        Assert.IsTrue(_service.ValidarDatosAlerta(alerta));
    }
}