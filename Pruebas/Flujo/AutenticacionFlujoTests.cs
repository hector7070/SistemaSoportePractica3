using Abstracciones.Excepciones;
using Abstracciones.Seguridad;
using Flujo.Seguridad;
using Moq;

namespace Pruebas;

/// <summary>
/// Pruebas unitarias de AutenticacionFlujo usando Moq para simular IUsuarioDA e IGeneradorToken.
/// Patrón: Arrange (preparar) / Act (ejecutar) / Assert (verificar).
/// </summary>
public class AutenticacionFlujoTests
{
    private readonly Mock<IUsuarioDA> _usuarioDa = new();
    private readonly Mock<IGeneradorToken> _generadorToken = new();
    private readonly AutenticacionFlujo _flujo;

    public AutenticacionFlujoTests()
    {
        _flujo = new AutenticacionFlujo(_usuarioDa.Object, _generadorToken.Object);
    }

    [Fact]
    public async Task IniciarSesion_ConCredencialesCorrectas_DevuelveSesionYGeneraTokenUnaVez()
    {
        // Arrange
        const string password = "PasswordValido123!";
        var usuario = new Usuario
        {
            Id = 1,
            Nombre = "Usuario Prueba",
            Correo = "usuario@soporte.local",
            ClaveHash = HasherPassword.Hashear(password),
            Rol = RolUsuario.Admin,
            Activo = true
        };

        var fechaExpiracion = DateTime.UtcNow.AddHours(2);
        const string tokenEsperado = "token.jwt.simulado";

        _usuarioDa.Setup(d => d.ObtenerPorCorreoAsync("usuario@soporte.local", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(usuario);

        _generadorToken.Setup(g => g.Generar(usuario))
                       .Returns((tokenEsperado, fechaExpiracion));

        var dto = new LoginDto
        {
            Correo = "usuario@soporte.local",
            Password = password
        };

        // Act
        var sesion = await _flujo.IniciarSesionAsync(dto, CancellationToken.None);

        // Assert
        Assert.NotNull(sesion);
        Assert.Equal(tokenEsperado, sesion.Token);
        Assert.Equal(RolUsuario.Admin.ToString(), sesion.Rol);
        Assert.Equal(usuario.Correo, sesion.Correo);
        Assert.Equal(usuario.Nombre, sesion.Nombre);
        Assert.Equal(fechaExpiracion, sesion.ExpiraEnUtc);

        _generadorToken.Verify(g => g.Generar(usuario), Times.Once);
    }

    [Fact]
    public async Task IniciarSesion_ConCorreoInexistente_LanzaCredencialesInvalidasYNoGeneraToken()
    {
        // Arrange
        _usuarioDa.Setup(d => d.ObtenerPorCorreoAsync("inexistente@soporte.local", It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Usuario?)null);

        var dto = new LoginDto
        {
            Correo = "inexistente@soporte.local",
            Password = "CualquierPassword123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _flujo.IniciarSesionAsync(dto, CancellationToken.None));

        _generadorToken.Verify(g => g.Generar(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task IniciarSesion_ConContrasenaIncorrecta_LanzaCredencialesInvalidasYNoGeneraToken()
    {
        // Arrange
        var usuario = new Usuario
        {
            Id = 1,
            Nombre = "Usuario Prueba",
            Correo = "usuario@soporte.local",
            ClaveHash = HasherPassword.Hashear("PasswordCorrecto123!"),
            Rol = RolUsuario.Agente,
            Activo = true
        };

        _usuarioDa.Setup(d => d.ObtenerPorCorreoAsync("usuario@soporte.local", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(usuario);

        var dto = new LoginDto
        {
            Correo = "usuario@soporte.local",
            Password = "PasswordErroneo123!"
        };

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _flujo.IniciarSesionAsync(dto, CancellationToken.None));

        _generadorToken.Verify(g => g.Generar(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task IniciarSesion_ConUsuarioInactivo_LanzaCredencialesInvalidasYNoGeneraToken()
    {
        // Arrange
        const string password = "PasswordCorrecto123!";
        var usuario = new Usuario
        {
            Id = 1,
            Nombre = "Usuario Inactivo",
            Correo = "inactivo@soporte.local",
            ClaveHash = HasherPassword.Hashear(password),
            Rol = RolUsuario.Agente,
            Activo = false // inactivo
        };

        _usuarioDa.Setup(d => d.ObtenerPorCorreoAsync("inactivo@soporte.local", It.IsAny<CancellationToken>()))
                  .ReturnsAsync(usuario);

        var dto = new LoginDto
        {
            Correo = "inactivo@soporte.local",
            Password = password
        };

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            _flujo.IniciarSesionAsync(dto, CancellationToken.None));

        _generadorToken.Verify(g => g.Generar(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task IniciarSesion_NormalizaCorreoAntesDeConsultarAlDA()
    {
        // Arrange
        const string correoEntrada = "  Admin@Soporte.Local  ";
        const string correoNormalizado = "admin@soporte.local";
        const string password = "PasswordValido123!";

        var usuario = new Usuario
        {
            Id = 1,
            Nombre = "Administrador",
            Correo = correoNormalizado,
            ClaveHash = HasherPassword.Hashear(password),
            Rol = RolUsuario.Admin,
            Activo = true
        };

        _usuarioDa.Setup(d => d.ObtenerPorCorreoAsync(correoNormalizado, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(usuario);

        _generadorToken.Setup(g => g.Generar(usuario))
                       .Returns(("token.jwt.simulado", DateTime.UtcNow.AddHours(1)));

        var dto = new LoginDto
        {
            Correo = correoEntrada,
            Password = password
        };

        // Act
        await _flujo.IniciarSesionAsync(dto, CancellationToken.None);

        // Assert
        _usuarioDa.Verify(d => d.ObtenerPorCorreoAsync(correoNormalizado, It.IsAny<CancellationToken>()), Times.Once);
    }
}
