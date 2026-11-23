using System.Net;
using System.Text.Json;
using Web.Models;

namespace Web.Servicios;

public class EtiquetasApi : IEtiquetasApi
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public EtiquetasApi(HttpClient http) => _http = http;

    public async Task<List<EtiquetaVm>> ListarAsync(CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync("api/etiquetas", ct), ct);
        await ApiException.LanzarSiFallaAsync(respuesta, ct);
        return await respuesta.Content.ReadFromJsonAsync<List<EtiquetaVm>>(OpcionesJson, ct) ?? new();
    }

    public async Task<EtiquetaVm?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(() => _http.GetAsync($"api/etiquetas/{id}", ct), ct);
        if (respuesta.StatusCode == HttpStatusCode.NotFound) return null;

        await ApiException.LanzarSiFallaAsync(respuesta, ct);
        return await respuesta.Content.ReadFromJsonAsync<EtiquetaVm>(OpcionesJson, ct);
    }

    public async Task<EtiquetaVm> CrearAsync(EtiquetaCrearVm datos, CancellationToken ct = default)
    {
        var cuerpo = new { datos.Nombre, datos.Color };
        using var respuesta = await EnviarAsync(() => _http.PostAsJsonAsync("api/etiquetas", cuerpo, OpcionesJson, ct), ct);
        await ApiException.LanzarSiFallaAsync(respuesta, ct);
        return (await respuesta.Content.ReadFromJsonAsync<EtiquetaVm>(OpcionesJson, ct))!;
    }

    private static async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> peticion, CancellationToken ct)
    {
        try
        {
            return await peticion();
        }
        catch (HttpRequestException)
        {
            throw new ApiException(null, "No se pudo conectar con el API",
                "Verifique que el API esté corriendo y que Api:UrlBase sea correcta.");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new ApiException(null, "El API tardó demasiado",
                "Si usa Azure SQL gratis, la base pudo estar pausada. Intente de nuevo.");
        }
    }
}
