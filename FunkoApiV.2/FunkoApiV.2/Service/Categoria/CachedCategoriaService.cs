using CSharpFunctionalExtensions;
using FunkoApiMongo.Cache;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Repository;

namespace FunkoApiV._2.Service;

public class CachedCategoriaService(
    ICategoriaService inner,
    ICacheService cache
) : ICategoriaService {
    private static string ByIdKey(string id) => $"categoria:{id}";

    /// <summary>
    /// Los listados van paginados y filtrados: no se cachean.
    /// </summary>
    public Task<PagedResult<CategoriaDto>> GetAll(CategoriaFilterRequest filter) =>
        inner.GetAll(filter);

    public async Task<Result<CategoriaDto, DomainError>> GetById(string id) {
        var key = ByIdKey(id);
        var cached = await cache.GetAsync<CategoriaDto>(key);
        if (cached is not null)
            return Result.Success<CategoriaDto, DomainError>(cached);

        var result = await inner.GetById(id);
        if (result.IsSuccess)
            await cache.SetAsync(key, result.Value);
        return result;
    }

    public Task<Result<CategoriaDto, DomainError>> Add(CategoriaRequestDto request) =>
        inner.Add(request);

    public async Task<Result<CategoriaDto, DomainError>> Update(string id, CategoriaRequestDto request) {
        var result = await inner.Update(id, request);
        if (result.IsFailure)
            return result;

        await cache.RemoveAsync(ByIdKey(id));
        

        return result;
    }

    public async Task<Result<bool, DomainError>> Delete(string id) {
        var result = await inner.Delete(id);
        if (result.IsSuccess)
            await cache.RemoveAsync(ByIdKey(id));
        return result;
    }
}
