using System.ComponentModel.DataAnnotations;

namespace FunkoApiV._2.GraphQL.Inputs;

public record UpdateFunkoInput {
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(20, ErrorMessage = "El nombre no puede superar los 20 caracteres")]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
    public string Name { get; set; } = string.Empty;
    
    public decimal Precio { get; set; }
    
    [Required(ErrorMessage = "El stock es obligatorio")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; set; }
    
    [Required(ErrorMessage = "La categoría es obligatoria")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "El ID de categoría debe tener 11 caracteres")]
    public string CategoriaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    [RegularExpression("^[a-fA-F0-9]{24}$", ErrorMessage = "El ID del proveedor debe tener 24 caracteres hexadecimales")]
    public string ProveedorId { get; set; } = string.Empty;
    
    public string? Imagen { get; set; }
}