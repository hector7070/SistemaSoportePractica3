using Web.Models;

namespace Web.Servicios;

/// <summary>
/// Contrato para el consumo del módulo de Etiquetas del API por HTTP.
/// </summary>
public interface IEtiquetasApi
{
    Task<List<EtiquetaVm>> ListarAsync(CancellationToken ct = default);

    /// <summary>Devuelve null si el API responde 404 (no encontrada). Cualquier otro error lanza ApiException.</summary>
    Task<EtiquetaVm?> ObtenerAsync(int id, CancellationToken ct = default);

    Task<EtiquetaVm> CrearAsync(EtiquetaCrearVm datos, CancellationToken ct = default);
}
