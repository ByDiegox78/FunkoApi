using CSharpFunctionalExtensions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApiV._2.Repository;

public class FunkoRepository(AppDbContext context) : IFunkoRepository, IScopedService {
    public async Task<(IReadOnlyList<Funko> Items, int Total)> GetPagedAsync(FunkoFilterRequest filter) {
        IQueryable<Funko> query = context.Funkos.AsNoTracking();

        if (filter.IncludeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Include(f => f.Categoria);

        if (!string.IsNullOrWhiteSpace(filter.Nombre))
            query = query.Where(f => EF.Functions.ILike(f.Name, $"%{filter.Nombre}%"));

        if (!string.IsNullOrWhiteSpace(filter.CategoriaId))
            query = query.Where(f => f.CategoriaId == filter.CategoriaId);

        if (!string.IsNullOrWhiteSpace(filter.ProveedorId))
            query = query.Where(f => f.ProveedorId == filter.ProveedorId);

        if (filter.PrecioMin.HasValue)
            query = query.Where(f => f.Precio >= filter.PrecioMin.Value);

        if (filter.PrecioMax.HasValue)
            query = query.Where(f => f.Precio <= filter.PrecioMax.Value);

        if (filter.StockMin.HasValue)
            query = query.Where(f => f.Stock >= filter.StockMin.Value);

        var total = await query.CountAsync();
        var items = await ApplySort(query, filter.SortBy, filter.Direction)
            .Skip(filter.Page * filter.Size)
            .Take(filter.Size)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Result<Funko, DomainError>> GetByIdAsync(long id) {
        var funko = await context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == id);
        return funko is not null
            ? Result.Success<Funko, DomainError>(funko)
            : Result.Failure<Funko, DomainError>(NotFound.FromId(id.ToString(), "Funko"));
    }

    public async Task<Result<Funko, DomainError>> CreateAsync(Funko funko) {
        var nuevo = funko with {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            IsDeleted = false
        };
        await context.Funkos.AddAsync(nuevo);
        await context.SaveChangesAsync();
        await context.Entry(nuevo).Reference(f => f.Categoria).LoadAsync();
        return Result.Success<Funko, DomainError>(nuevo);
    }

    public async Task<Result<Funko, DomainError>> UpdateAsync(long id, Funko funko) {
        var existente = await context.Funkos.FirstOrDefaultAsync(f => f.Id == id);
        if (existente is null)
            return Result.Failure<Funko, DomainError>(NotFound.FromId(id.ToString(), "Funko"));

        existente.Name = funko.Name;
        existente.Precio = funko.Precio;
        existente.Stock = funko.Stock;
        existente.Imagen = funko.Imagen;
        existente.CategoriaId = funko.CategoriaId;
        existente.ProveedorId = funko.ProveedorId;
        existente.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await context.Entry(existente).Reference(f => f.Categoria).LoadAsync();
        return Result.Success<Funko, DomainError>(existente);
    }

    public async Task<Result<Funko, DomainError>> SetImagenAsync(long id, string imagen) {
        var existente = await context.Funkos.FirstOrDefaultAsync(f => f.Id == id);
        if (existente is null)
            return Result.Failure<Funko, DomainError>(NotFound.FromId(id.ToString(), "Funko"));

        existente.Imagen = imagen;
        existente.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        await context.Entry(existente).Reference(f => f.Categoria).LoadAsync();
        return Result.Success<Funko, DomainError>(existente);
    }

    public async Task<Result<bool, DomainError>> DeleteAsync(long id) {
        var funko = await context.Funkos.FirstOrDefaultAsync(f => f.Id == id);
        if (funko is null)
            return Result.Failure<bool, DomainError>(NotFound.FromId(id.ToString(), "Funko"));

        funko.IsDeleted = true;
        funko.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success<bool, DomainError>(true);
    }

    private static IQueryable<Funko> ApplySort(IQueryable<Funko> query, string sortBy, string direction) {
        var desc = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch {
            "name" or "nombre" => desc ? query.OrderByDescending(f => f.Name) : query.OrderBy(f => f.Name),
            "precio" or "price" => desc ? query.OrderByDescending(f => f.Precio) : query.OrderBy(f => f.Precio),
            "stock" => desc ? query.OrderByDescending(f => f.Stock) : query.OrderBy(f => f.Stock),
            "createdat" or "created_at" => desc ? query.OrderByDescending(f => f.CreatedAt) : query.OrderBy(f => f.CreatedAt),
            _ => desc ? query.OrderByDescending(f => f.Id) : query.OrderBy(f => f.Id)
        };
    }
}
