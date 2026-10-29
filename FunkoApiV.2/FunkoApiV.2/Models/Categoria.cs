using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunkoApiV._2.Models;

public record Categoria {
    [YoutubeId] 
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; } = string.Empty;
    public ICollection<Funko> Funkos { get; set; } = [];
}