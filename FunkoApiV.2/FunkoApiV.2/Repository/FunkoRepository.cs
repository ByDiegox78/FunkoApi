using System;
using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using FunkoApiV._2.Entity;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Models;
using Microsoft.EntityFrameworkCore;

namespace FunkoApiV._2.Repository;

public class FunkoRepository(AppDbContext context) : IFunkoRepository, IScopedService {
    public async Task<IReadOnlyList<Funko>> GetAll() => 
        await context.Funkos
            .AsNoTracking()
            .Include(f => f.Categoria)
            .ToListAsync();
    

    public async Task<Result<Funko, DomainError>> GetById(int id) {
        var funko = await context.Funkos.AsNoTracking().Include(c => c.Categoria).FirstOrDefaultAsync(f => f.Id == id);
        return funko is not null
            ? Result.Success<Funko, DomainError>(funko)
            : Result.Failure<Funko, DomainError>(NotFound.FromId(id, "Funko"));
    }
    public async Task<IReadOnlyList<Funko>> GetAllDeleted() {
        return await context.Funkos.IgnoreQueryFilters()
            .AsNoTracking()
            .Include(c => c.Categoria)
            .Where(f => f.IsDeleted)
            .ToListAsync();
    }

    public async Task<Result<Funko, DomainError>> Add(Funko funko) {
        var nuevo = funko with {
            CreatedAt = DateTime.Now,
            IsDeleted = false
        };
        await context.Funkos.AddAsync(nuevo);
        await context.SaveChangesAsync();
        return Result.Success<Funko, DomainError>(nuevo);
    }

    public async Task<Result<Funko, DomainError>> Update(int id, Funko funko) {
        var existente = await context.Funkos
            .Include(f => f.Categoria)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (existente is null || existente.IsDeleted)
            return Result.Failure<Funko, DomainError>(NotFound.FromId(id, "Funko"));
        existente.Name = funko.Name;
        existente.CategoriaId = funko.CategoriaId;
        existente.Precio = funko.Precio;
        existente.Stock = funko.Stock;
        existente.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success<Funko, DomainError>(existente);
    }
    public async Task<Result<bool, DomainError>> SoftDelete(int id) {
        var funko = await context.Funkos.FindAsync(id);
        if (funko is null) 
            return Result.Failure<bool, DomainError>(NotFound.FromId(id, "Funko"));
        funko.IsDeleted = true;
        funko.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Result.Success<bool, DomainError>(true);
    }

    public async Task<Result<bool, DomainError>> Restore(int id) {
        var funko = await context.Funkos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id  == id && f.IsDeleted);
        if (funko is null) 
            return Result.Failure<bool, DomainError>(NotFound.FromId(id, "Funko"));
        funko.IsDeleted = false;
        await context.SaveChangesAsync();
        return Result.Success<bool, DomainError>(true);
    }
}