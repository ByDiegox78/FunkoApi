using System;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Mapper;

public static class FunkoMapper {
    public static FunkoResponseDto ToDto(this Funko f) => new() {
        Id = f.Id,
        Name = f.Name,
        Precio = f.Precio,
        Stock = f.Stock,
        CategoriaId = f.CategoriaId,
        Categoria = f.Categoria?.Nombre ?? string.Empty,
        CreatedAt = f.CreatedAt,
        UpdatedAt = f.UpdatedAt
    };

    public static Funko ToModel(this FunkoCreateRequest dto) => new() {
        Name = dto.Name,
        Precio = dto.Precio,
        Stock = dto.Stock,
        CategoriaId = dto.CategoriaId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = null,
        IsDeleted = false
    };

    public static Funko ToModel(this FunkoUpdateRequest dto) => new() {
        Name = dto.Name,
        Precio = dto.Precio,
        Stock = dto.Stock,
        CategoriaId = dto.CategoriaId,
        UpdatedAt = DateTime.UtcNow
    };

    public static CategoriaDto ToDto(this Categoria c) => new(
        c.Id,
        c.Nombre,
        c.Descripcion ?? string.Empty
    );
}