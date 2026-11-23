using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Servicios;

namespace Web.Controllers;

/// <summary>
/// Controlador MVC para la gestión de Etiquetas (lista, detalle y creación).
/// Consume el API mediante el cliente tipado IEtiquetasApi y aplica el patrón Post-Redirect-Get.
/// </summary>
public class EtiquetasController : Controller
{
    private readonly IEtiquetasApi _api;

    public EtiquetasController(IEtiquetasApi api) => _api = api;

    // ---------------- Lectura ----------------

    public async Task<IActionResult> Index(CancellationToken ct = default) =>
        View(await _api.ListarAsync(ct));

    public async Task<IActionResult> Detalle(int id, CancellationToken ct = default)
    {
        var etiqueta = await _api.ObtenerAsync(id, ct);
        return etiqueta is null ? NotFound() : View(etiqueta);
    }

    // ---------------- Creación ----------------

    [HttpGet]
    public IActionResult Crear() => View(new EtiquetaCrearVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(EtiquetaCrearVm datos, CancellationToken ct)
    {
        // 1) Validación previa en el cliente/servidor MVC con las anotaciones del ViewModel
        if (!ModelState.IsValid) return View(datos);

        try
        {
            var creada = await _api.CrearAsync(datos, ct);
            TempData["Exito"] = $"Etiqueta '{creada.Nombre}' creada con éxito.";

            // Post-Redirect-Get: redirige a la vista Detalle con el identificador generado
            return RedirectToAction(nameof(Detalle), new { id = creada.Id });
        }
        // 2) Atrapa errores de validación o conflicto provenientes del API
        catch (ApiException ex) when (ex.Codigo is 400 or 409 or 422)
        {
            AgregarErroresDelApi(ex);
            return View(datos);
        }
    }

    private void AgregarErroresDelApi(ApiException ex)
    {
        if (ex.Errores.Count == 0)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        foreach (var (campo, mensajes) in ex.Errores)
        {
            foreach (var mensaje in mensajes)
            {
                ModelState.AddModelError(campo, mensaje);
            }
        }
    }
}
