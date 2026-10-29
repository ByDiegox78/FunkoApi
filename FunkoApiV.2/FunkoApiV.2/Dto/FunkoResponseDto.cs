using System;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Dto;

public record FunkoResponseDto {
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public int CategoriaId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}