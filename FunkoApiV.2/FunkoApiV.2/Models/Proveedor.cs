using MongoDB.Bson;

namespace FunkoApiV._2.Models;
public record Proveedor {
    public ObjectId Id { get; init; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public ICollection<Funko> Funkos { get; set; } = []; 
}
