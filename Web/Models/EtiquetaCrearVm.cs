using System.ComponentModel.DataAnnotations;

namespace Web.Models;

/// <summary>
/// Datos del formulario para crear una nueva etiqueta.
/// Mantiene las mismas reglas de validación definidas en el API.
/// </summary>
public class EtiquetaCrearVm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(40, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 40 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El color es obligatorio.")]
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "El color debe ser hexadecimal, ej. #ff8800.")]
    public string Color { get; set; } = "#6c757d";
}
