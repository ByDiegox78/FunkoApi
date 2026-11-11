using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Mapper;
using FunkoApiV._2.Repository;
using HotChocolate;

namespace FunkoApiV._2.GraphQL.Queries;

public class FunkoQuery {
    /// <summary>
    /// Listado paginado de funkos con filtros opcionales.
    /// Antes cargaba la tabla completa en memoria; ahora usa la misma consulta
    /// paginada que la API REST para no traer más filas de las necesarias.
    /// </summary>
    public async Task<PagedResult<FunkoDto>> GetFunkos(
        [Service] IFunkoRepository funkoRepository,
        int page = 1,
        int size = 10,
        string? nombre = null,
        string? categoriaId = null,
        string? proveedorId = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        int? stockMin = null,
        string sortBy = "id",
        string direction = "asc",
        bool includeDeleted = false) {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var filter = new FunkoFilterRequest {
            Nombre = nombre,
            CategoriaId = categoriaId,
            ProveedorId = proveedorId,
            PrecioMin = precioMin,
            PrecioMax = precioMax,
            StockMin = stockMin,
            Page = page - 1,
            Size = size,
            SortBy = sortBy,
            Direction = direction,
            IncludeDeleted = includeDeleted
        };
        var result = await funkoRepository.GetPagedAsync(filter);

        return new PagedResult<FunkoDto> {
            Items = result.Items.Select(funko => funko.ToFunkoDto()).ToList(),
            TotalCount = result.Total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<FunkoDto?> GetFunkoById(
        long id,
        [Service] IFunkoRepository funkoRepository) {
        var result = await funkoRepository.GetByIdAsync(id);
        return result.IsSuccess ? result.Value.ToFunkoDto() : null;
    }

    public async Task<PagedResult<CategoriaDto>> GetCategoriasPaged(
        [Service] ICategoriaRepository categoriaRepository,
        int page = 1,
        int size = 10) {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);
        var filter = new CategoriaFilterRequest {
            Page = page - 1,
            Size = size
        };
        var result = await categoriaRepository.GetPagedAsync(filter);

        return new PagedResult<CategoriaDto> {
            Items = result.Items.Select(categoria => categoria.ToDto()).ToList(),
            TotalCount = result.Total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<CategoriaDto?> GetCategoriaById(
        string id,
        [Service] ICategoriaRepository categoriaRepository) {
        var result = await categoriaRepository.GetById(id);
        return result.IsSuccess ? result.Value.ToDto() : null;
    }
}
