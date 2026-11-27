using Abstracciones.Etiquetas;
using Abstracciones.Excepciones;
using Flujo.Etiquetas;
using Moq;

namespace Pruebas;

/// <summary>
/// Pruebas unitarias de EtiquetaFlujo usando Moq para simular IEtiquetaDA.
/// Patrón: Arrange (preparar) / Act (ejecutar) / Assert (verificar).
/// </summary>
public class EtiquetaFlujoTests
{
    private readonly Mock<IEtiquetaDA> _da = new();
    private readonly EtiquetaFlujo _flujo;

    public EtiquetaFlujoTests()
    {
        _flujo = new EtiquetaFlujo(_da.Object);
    }

    [Fact]
    public async Task Crear_NormalizaNombreYColor()
    {
        // Arrange
        var dto = new EtiquetaCrearDto
        {
            Nombre = "  Urgente  ",
            Color = "#FF00AA"
        };

        _da.Setup(d => d.ObtenerPorNombreAsync("Urgente", It.IsAny<CancellationToken>()))
           .ReturnsAsync((Etiqueta?)null);

        _da.Setup(d => d.AgregarAsync(It.IsAny<Etiqueta>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((Etiqueta e, CancellationToken _) => new Etiqueta
           {
               Id = 1,
               Nombre = e.Nombre,
               Color = e.Color
           });

        // Act
        var resultado = await _flujo.CrearAsync(dto, CancellationToken.None);

        // Assert
        Assert.Equal("Urgente", resultado.Nombre);
        Assert.Equal("#ff00aa", resultado.Color);

        _da.Verify(d => d.AgregarAsync(
            It.Is<Etiqueta>(e => e.Nombre == "Urgente" && e.Color == "#ff00aa"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Crear_ConNombreRepetido_LanzaConflictoYNoLlamaAgregar()
    {
        // Arrange
        var dto = new EtiquetaCrearDto
        {
            Nombre = "  Facturación  ",
            Color = "#00ff00"
        };

        _da.Setup(d => d.ObtenerPorNombreAsync("Facturación", It.IsAny<CancellationToken>()))
           .ReturnsAsync(new Etiqueta { Id = 10, Nombre = "Facturación", Color = "#00ff00" });

        // Act & Assert
        await Assert.ThrowsAsync<ConflictoException>(() =>
            _flujo.CrearAsync(dto, CancellationToken.None));

        _da.Verify(d => d.AgregarAsync(It.IsAny<Etiqueta>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Obtener_ConEtiquetaInexistente_LanzaNoEncontrado()
    {
        // Arrange
        _da.Setup(d => d.ObtenerPorIdAsync(999, It.IsAny<CancellationToken>()))
           .ReturnsAsync((Etiqueta?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NoEncontradoException>(() =>
            _flujo.ObtenerAsync(999, CancellationToken.None));
    }
}
