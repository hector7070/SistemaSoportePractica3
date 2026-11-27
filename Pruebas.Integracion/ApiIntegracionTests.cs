using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Abstracciones.Seguridad;

namespace Pruebas.Integracion;

/// <summary>
/// Pruebas de integración: el API arranca en memoria y se le hacen peticiones HTTP reales (sin red).
/// Patrón de cada prueba: Arrange (preparar) / Act (ejecutar) / Assert (verificar).
/// </summary>
public class ApiIntegracionTests : IClassFixture<FabricaApiPrueba>
{
    private readonly HttpClient _cliente;

    public ApiIntegracionTests(FabricaApiPrueba fabrica)
    {
        _cliente = fabrica.CreateClient();
    }

    [Fact]
    public async Task Login_ConCredencialesIncorrectas_Devuelve401ConProblemDetails()
    {
        // Arrange
        var login = new LoginDto { Correo = "nadie@soporte.local", Password = "clave-incorrecta" };

        // Act
        var respuesta = await _cliente.PostAsJsonAsync("/api/auth/login", login);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);

        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(401, cuerpo.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(cuerpo.RootElement.GetProperty("title").GetString()));
    }

    [Fact]
    public async Task RutaProtegida_SinToken_Devuelve401()
    {
        // Arrange: PUT /api/clientes/1 exige rol Admin o Supervisor; no se envía ningún token.
        var cuerpo = new { nombre = "Ana", correo = "ana@ejemplo.com", telefono = "88880001" };

        // Act
        var respuesta = await _cliente.PutAsJsonAsync("/api/clientes/1", cuerpo);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
