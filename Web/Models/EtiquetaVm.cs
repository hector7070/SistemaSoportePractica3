namespace Web.Models;

/// <summary>
/// ViewModel de una etiqueta. Representa la forma en que el proyecto Web consume el JSON del API.
/// </summary>
public class EtiquetaVm
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
