using CSharpFunctionalExtensions;
using FunkoApiMongo.Cache;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Service;

namespace FunkoApiMongo.Service;

public class CachedFunkoService(IFunkoService inner, ICacheService cache) : IFunkoService {
    private const string AllKey = "funkos:all";
    private const string DeletedKey = "funkos:deleted";
    private static string ByIdKey(int id) => $"funko:{id}";
    
    public async Task<IReadOnlyList<FunkoResponseDto>> GetAll() {
        var result = await cache.GetOrSetAsync(
            AllKey,
            async () => await inner.GetAll()
        );
        return result ?? [];
    }

    public async Task<IReadOnlyCollection<FunkoResponseDto>> GetDeleted() {
        var result = await cache.GetOrSetAsync(
            DeletedKey,
            async () => await inner.GetDeleted()
        );
        return result ?? [];
    }

    public async Task<Result<FunkoResponseDto, DomainError>> GetById(int id) {
        var key = ByIdKey(id);
        var cached = await cache.GetAsync<FunkoResponseDto>(key);
        if (cached != null) 
            return Result.Success<FunkoResponseDto, DomainError>(cached);
        var result = await inner.GetById(id);
        if (result.IsSuccess)
            await cache.SetAsync(key, result.Value);
        return result;
    }

    public async Task<Result<FunkoResponseDto, DomainError>> Add(FunkoCreateRequest funko) {
        var res = await inner.Add(funko);
        if (res.IsSuccess) {
            await cache.RemoveAsync(AllKey);
        }
        return res;
    }

    public async Task<Result<FunkoResponseDto, DomainError>> Update(int id, FunkoUpdateRequest funko) {
        var res = await inner.Update(id, funko);
        if (!res.IsSuccess) return res;
        await cache.RemoveAsync(ByIdKey(id));
        await cache.RemoveAsync(AllKey);
        return res;
    }

    public async Task<Result<bool, DomainError>> SoftDelete(int id) {
        var res = await inner.SoftDelete(id);
        if (!res.IsSuccess) return res;
        await cache.RemoveAsync(ByIdKey(id));
        await cache.RemoveAsync(AllKey);
        await cache.RemoveAsync(DeletedKey);
        return res;
        
    }

    public async Task<Result<bool, DomainError>> Restore(int id) {
        var res = await inner.Restore(id);
        if (!res.IsSuccess) return res;

        await cache.RemoveAsync(ByIdKey(id));
        await cache.RemoveAsync(AllKey);
        await cache.RemoveAsync(DeletedKey);
        return res;
    }

    public async Task<IReadOnlyList<CategoriaDto>> GetAllCategories() {
        var result = await cache.GetOrSetAsync(
            "categorias:all",
            async () => await inner.GetAllCategories()
        );
        return result ?? [];
    }

    public async Task<Result<CategoriaDto, DomainError>> GetByIdCategories(int id) {
        var key = $"categoria:{id}";
        var cached = await cache.GetAsync<CategoriaDto>(key);
        if (cached != null) 
            return Result.Success<CategoriaDto, DomainError>(cached);
            
        var result = await inner.GetByIdCategories(id);
        
        if (result.IsSuccess)
            await cache.SetAsync(key, result.Value);
            
        return result;
    }
}