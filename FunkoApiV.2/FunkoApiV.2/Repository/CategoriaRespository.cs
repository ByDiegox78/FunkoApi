using CSharpFunctionalExtensions;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApiV._2.Repository;

public class CategoriaRespository(AppDbContext context) : ICategoriaRepository, IScopedService {
    public async Task<IReadOnlyList<Categoria>> GetAll() {
        return await context.Categorias.AsNoTracking().ToListAsync();
    }
    public async Task<Result<Categoria, DomainError>> GetById(int id) {
        var categoria = await context.Categorias.FindAsync(id);
        return categoria is not null
            ? Result.Success<Categoria, DomainError>(categoria)
            : Result.Failure<Categoria, DomainError>(NotFound.FromId(id, "Funko"));
    }
}