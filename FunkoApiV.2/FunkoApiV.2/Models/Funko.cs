using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MongoDB.Bson;

namespace FunkoApiV._2.Models;

public record Funko {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public string? Imagen { get; set; }
    
    public string CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;
    
    public ObjectId ProveedorId { get; set; } 
    public Proveedor Proveedor { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}