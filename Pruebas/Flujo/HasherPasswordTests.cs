using Abstracciones.Seguridad;

namespace Pruebas;

/// <summary>
/// Pruebas unitarias de HasherPassword. Es una función pura (sin DA, sin base de datos, sin red),
/// por eso no se necesita Moq.
/// Patrón de cada prueba: Arrange (preparar) / Act (ejecutar) / Assert (verificar).
/// </summary>
public class HasherPasswordTests
{
    [Fact]
    public void Hashear_NoDevuelveLaContrasenaEnTextoPlano()
    {
        // Arrange
        const string password = "Soporte2026!";

        // Act
        var hash = HasherPassword.Hashear(password);

        // Assert
        Assert.NotEqual(password, hash);
        Assert.DoesNotContain(password, hash);
    }

    [Fact]
    public void Hashear_ConLaMismaContrasena_GeneraHashesDistintos()
    {
        // Arrange
        const string password = "Soporte2026!";

        // Act
        var hash1 = HasherPassword.Hashear(password);
        var hash2 = HasherPassword.Hashear(password);

        // Assert
        // Cada llamada genera una sal aleatoria distinta, así que el resultado cambia aunque la contraseña sea igual.
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verificar_ConLaContrasenaCorrecta_DevuelveTrue()
    {
        // Arrange
        var hash = HasherPassword.Hashear("laclave");

        // Act
        var resultado = HasherPassword.Verificar("laclave", hash);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void Verificar_ConUnaContrasenaIncorrecta_DevuelveFalse()
    {
        // Arrange
        var hash = HasherPassword.Hashear("laclave");

        // Act
        var resultado = HasherPassword.Verificar("otraclave", hash);

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public void Verificar_ConHashDeFormatoCorrupto_DevuelveFalse()
    {
        // Arrange
        const string hashCorrupto = "abc"; // no tiene las 3 partes iteraciones.sal.hash

        // Act
        var resultado = HasherPassword.Verificar("laclave", hashCorrupto);

        // Assert
        Assert.False(resultado);
    }
}
