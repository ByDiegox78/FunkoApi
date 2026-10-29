using System.ComponentModel.DataAnnotations;

namespace FunkoApiV._2.Dto;

public record FunkoCreateRequest {
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(20, ErrorMessage = "El nombre solo puede tener 20 caracteres")]
    [MinLength(2, ErrorMessage = "El nombre debe tener 2 caracteres al menos")]
    public string Name { get; init; } = string.Empty;
        
    [Range(0.01, 50.0, ErrorMessage = "El precio debe estar entre 0.01 y 50.0")]
    public decimal Precio { get; init; }
    
    [Range(1, int.MaxValue, ErrorMessage = "El ID de la categoría debe ser mayor que 0")]
    public int CategoriaId { get; init; } 
    
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; init; }
}