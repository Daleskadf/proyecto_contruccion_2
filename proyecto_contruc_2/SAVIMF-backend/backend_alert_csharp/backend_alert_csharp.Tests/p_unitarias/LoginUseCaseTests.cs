using NUnit.Framework;
using Moq;
using Application.UseCases;
using Domain.Entities;
using Domain.Interfaces;
using System.Threading.Tasks;

[TestFixture]
public class LoginUseCaseTests
{
    [Test]
    public async Task EjecutarAsync_TokenValidoYConRol_RetornaUsuario()
    {
        // Arrange
        var token = "valid_token";
        var userDto = new UsuarioFirebaseDto { Uid = "uid123", Email = "user@email.com" };
        var mockAuth = new Mock<IFirebaseAuthService>();
        mockAuth.Setup(a => a.VerifyIdTokenAsync(token)).ReturnsAsync(userDto);

        var mockUserRepo = new Mock<IUserRepositoryFirestore>();
        mockUserRepo.Setup(r => r.GetRoleByUidAsync("uid123")).ReturnsAsync("admin");

        var useCase = new LoginUseCase(mockAuth.Object, mockUserRepo.Object);

        // Act
        var usuario = await useCase.EjecutarAsync(token);

        // Assert
        Assert.IsNotNull(usuario);
        Assert.That(usuario.Uid, Is.EqualTo("uid123"));
        Assert.That(usuario.Email, Is.EqualTo("user@email.com"));
        Assert.That(usuario.Role, Is.EqualTo("admin"));
    }

    [Test]
    public async Task EjecutarAsync_TokenValidoSinRol_RetornaNull()
    {
        // Arrange
        var token = "valid_token";
        var userDto = new UsuarioFirebaseDto { Uid = "uid123", Email = "user@email.com" };
        var mockAuth = new Mock<IFirebaseAuthService>();
        mockAuth.Setup(a => a.VerifyIdTokenAsync(token)).ReturnsAsync(userDto);

        var mockUserRepo = new Mock<IUserRepositoryFirestore>();
        mockUserRepo.Setup(r => r.GetRoleByUidAsync("uid123")).ReturnsAsync((string?)null);

        var useCase = new LoginUseCase(mockAuth.Object, mockUserRepo.Object);

        // Act
        var usuario = await useCase.EjecutarAsync(token);

        // Assert
        Assert.IsNull(usuario);
    }
}