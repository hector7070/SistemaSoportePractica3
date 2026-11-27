using Abstracciones.Seguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pruebas.Integracion;

/// <summary>
/// Levanta el API completo EN MEMORIA para las pruebas. No usa SQL Server, user-secrets ni red:
///  - Entorno "Testing": no se cargan los User Secrets de la máquina.
///  - La clave JWT es una clave de PRUEBA inyectada por configuración (no es ningún secreto real).
///  - No se migra la base al iniciar, y IUsuarioDA se reemplaza por un doble en memoria (sin usuarios).
/// </summary>
public class FabricaApiPrueba : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting entra a la configuración ANTES de que Program.cs la lea.
        builder.UseSetting("Jwt:Clave", "clave-solo-para-pruebas-de-integracion-0123456789");
        builder.UseSetting("Seguridad:Exigir", "true");
        builder.UseSetting("Base:MigrarAlIniciar", "false");

        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<IUsuarioDA>();
            servicios.AddScoped<IUsuarioDA, UsuarioDAEnMemoria>();
        });
    }

    /// <summary>Repositorio de usuarios vacío: cualquier correo "no existe".</summary>
    private sealed class UsuarioDAEnMemoria : IUsuarioDA
    {
        public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct = default) =>
            Task.FromResult<Usuario?>(null);
    }
}
