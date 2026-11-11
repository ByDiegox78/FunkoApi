using System.ComponentModel.DataAnnotations;

namespace FunkoApiV._2.Dto;

/// <summary>Representación de lectura de un Funko.</summary>
/// <param name="Id">Identificador numérico de 64 bits.</param>
/// <param name="Name">Nombre del producto.</param>
/// <param name="Precio">Precio en la moneda de la tienda.</param>
/// <param name="Stock">Unidades disponibles.</param>
/// <param name="Imagen">Ruta relativa de la imagen, si existe.</param>
/// <param name="Categoria">Nombre de la categoría.</param>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="ProveedorId">ObjectId del proveedor.</param>
/// <param name="CreatedAt">Fecha de creación UTC.</param>
/// <param name="UpdatedAt">Fecha de última actualización UTC, si se actualizó.</param>
public record FunkoDto(
    long Id,
    string Name,
    decimal Precio,
    int Stock,
    string? Imagen,
    string Categoria,
    string CategoriaId,
    string ProveedorId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Respuesta REST con los datos del Funko y su categoría.</summary>
/// <param name="Id">Identificador numérico de 64 bits.</param>
/// <param name="Name">Nombre del producto.</param>
/// <param name="Precio">Precio en la moneda de la tienda.</param>
/// <param name="Stock">Unidades disponibles.</param>
/// <param name="Imagen">Ruta relativa de la imagen, si existe.</param>
/// <param name="Categoria">Nombre de la categoría.</param>
/// <param name="CategoriaId">Identificador de la categoría.</param>
/// <param name="ProveedorId">ObjectId del proveedor.</param>
/// <param name="CreatedAt">Fecha de creación UTC.</param>
/// <param name="UpdatedAt">Fecha de última actualización UTC, si se actualizó.</param>
public record FunkoResponseDto(
    long Id,
    string Name,
    decimal Precio,
    int Stock,
    string? Imagen,
    string Categoria,
    string CategoriaId,
    string ProveedorId,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Entrada requerida para crear un Funko.</summary>
public record FunkoCreateRequest {
    /// <summary>Nombre de 2 a 20 caracteres.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(20, ErrorMessage = "El nombre solo puede tener 20 caracteres")]
    [MinLength(2, ErrorMessage = "El nombre debe tener 2 caracteres al menos")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Precio entre 0,01 y 50,00.</summary>
    public decimal Precio { get; init; }

    /// <summary>Identificador existente de categoría; exactamente 11 caracteres.</summary>
    [Required(ErrorMessage = "El ID de la categoría es obligatorio")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "El ID de la categoría debe tener 11 caracteres")]
    public string CategoriaId { get; init; } = string.Empty;

    /// <summary>ObjectId existente del proveedor; 24 caracteres hexadecimales.</summary>
    [Required(ErrorMessage = "El ID del proveedor es obligatorio")]
    [RegularExpression("^[a-fA-F0-9]{24}$", ErrorMessage = "El ID del proveedor debe ser un ObjectId de 24 caracteres hexadecimales")]
    public string ProveedorId { get; init; } = string.Empty;

    /// <summary>Unidades disponibles; no puede ser negativo.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; init; }

    /// <summary>Ruta de imagen obligatoria de hasta 255 caracteres.</summary>
    [Required(ErrorMessage = "La imagen es obligatoria")]
    [MaxLength(255, ErrorMessage = "La ruta de la imagen no puede superar los 255 caracteres")]
    public string Imagen { get; init; } = string.Empty;
}

/// <summary>Entrada requerida para actualizar un Funko.</summary>
public record FunkoUpdateRequest {
    /// <summary>Nombre de 2 a 20 caracteres.</summary>
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(20, ErrorMessage = "El nombre solo puede tener 20 caracteres")]
    [MinLength(2, ErrorMessage = "El nombre debe tener 2 caracteres al menos")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Precio entre 0,01 y 50,00.</summary>
    [Range(0.01, 50.0, ErrorMessage = "El precio debe estar entre 0.01 y 50.0")]
    public decimal Precio { get; init; }

    /// <summary>Unidades disponibles; no puede ser negativo.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
    public int Stock { get; init; }

    /// <summary>Ruta de imagen opcional de hasta 255 caracteres.</summary>
    [MaxLength(255, ErrorMessage = "La imagen no puede superar los 255 caracteres")]
    public string? Imagen { get; init; }

    /// <summary>Identificador existente de categoría; exactamente 11 caracteres.</summary>
    [Required(ErrorMessage = "El ID de la categoría es obligatorio")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "El ID de la categoría debe tener 11 caracteres")]
    public string CategoriaId { get; init; } = string.Empty;

    /// <summary>ObjectId existente del proveedor; 24 caracteres hexadecimales.</summary>
    [Required(ErrorMessage = "El ID del proveedor es obligatorio")]
    [RegularExpression("^[0-9a-fA-F]{24}$", ErrorMessage = "El ID del proveedor debe ser un ObjectId de 24 caracteres hex")]
    public string ProveedorId { get; init; } = string.Empty;
}

/// <summary>Filtros y paginación del listado de Funkos.</summary>
public record FunkoFilterRequest {
    /// <summary>Búsqueda parcial del nombre (sin distinguir mayúsculas/minúsculas).</summary>
    public string? Nombre { get; init; }
    /// <summary>Filtra por identificador exacto de categoría.</summary>
    public string? CategoriaId { get; init; }
    /// <summary>Filtra por ObjectId exacto del proveedor.</summary>
    public string? ProveedorId { get; init; }
    /// <summary>Precio mínimo inclusive.</summary>
    public decimal? PrecioMin { get; init; }
    /// <summary>Precio máximo inclusive.</summary>
    public decimal? PrecioMax { get; init; }
    /// <summary>Stock mínimo inclusive.</summary>
    public int? StockMin { get; init; }

    /// <summary>Página base cero.</summary>
    [Range(0, int.MaxValue, ErrorMessage = "La página no puede ser negativa")]
    public int Page { get; init; } = 0;

    /// <summary>Elementos por página, entre 1 y 100.</summary>
    [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
    public int Size { get; init; } = 10;

    /// <summary>Orden: id, name/nombre, precio/price, stock o createdat/created_at.</summary>
    public string SortBy { get; init; } = "id";
    /// <summary>Dirección asc o desc.</summary>
    public string Direction { get; init; } = "asc";
    /// <summary>Incluye también elementos eliminados lógicamente. Actualmente no requiere ADMIN.</summary>
    public bool IncludeDeleted { get; init; } = false;
}

public record ImagenResponseDto(string Imagen);
