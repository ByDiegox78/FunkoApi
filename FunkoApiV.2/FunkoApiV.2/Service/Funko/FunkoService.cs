using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Common;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Mapper;
using FunkoApiV._2.Models;
using FunkoApiV._2.Realtime;
using FunkoApiV._2.Repository;
using FunkoApiV._2.Repository.Cassandra;
using FunkoApiV._2.Storage;
using Microsoft.AspNetCore.SignalR;
using FunkoApiV._2.Validator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FunkoApiV._2.Service;

public class FunkoService(
    IFunkoRepository repo,
    IProveedorRepository repoProveedor,
    ICategoriaRepository  repoCategoria,
    ICassandraRepository cassandraRepo,
    ILogger<FunkoService> log,
    IValidator<FunkoCreateRequest> createValidation,
    IValidator<FunkoUpdateRequest> updateValidation,
    IStorageService storage,
    IHubContext<FunkosHub> hubContext
    ) : IFunkoService, IScopedService {

    private readonly ICassandraRepository? _cassandraRepo = cassandraRepo;
    private readonly ILogger<FunkoService> _log = log ?? NullLogger<FunkoService>.Instance;
    private readonly IValidator<FunkoCreateRequest> _createValidation = createValidation ?? new FunkoCreateValidator();
    private readonly IValidator<FunkoUpdateRequest> _updateValidation = updateValidation ?? new FunkoUpdateValidator();
    private readonly IStorageService _storage = storage ?? new NoopStorageService();

    private sealed class NoopStorageService : IStorageService {
        public Task<Result<string, DomainError>> SaveFileAsync(IFormFile file, string folder) =>
            Task.FromResult(Result.Success<string, DomainError>(string.Empty));

        public Task<Result<bool, DomainError>> DeleteFileAsync(string relativePath) =>
            Task.FromResult(Result.Success<bool, DomainError>(true));
    }

    public async Task<PagedResult<FunkoResponseDto>> GetAll(FunkoFilterRequest filter) {
        _log.LogInformation("Buscando funkos con filtros {@Filter}", filter);
        var (items, total) = await repo.GetPagedAsync(filter);
        return new PagedResult<FunkoResponseDto> {
            Items = items.Select(f => f.ToDto()).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.Size
        };
    }

    public async Task<Result<FunkoResponseDto, DomainError>> GetById(long id) {
        _log.LogInformation("Obteniendo funko por id: {@Id}", id);
        return (await repo.GetByIdAsync(id)).Map(f => f.ToDto());
    }

    public async Task<Result<FunkoResponseDto, DomainError>> Add(FunkoCreateRequest request) {
        _log.LogInformation("Creando funko: {@Funko}", request);
        
        var validation = await _createValidation.ValidateAsync(request);
        if (!validation.IsValid) {
            _log.LogWarning("Validación fallida al crear funko: {@Errors}", validation.Errors);
            return Result.Failure<FunkoResponseDto, DomainError>(
                new ValidationError(validation.Errors[0].ErrorMessage));
        }
        
        var categoria = await repoCategoria.GetById(request.CategoriaId);
        if (categoria.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("La categoría especificada no existe o está eliminada"));

        var proveedor = await repoProveedor.GetById(request.ProveedorId);
        if (proveedor.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("El proveedor especificado no existe o está eliminado"));

        
        var creado = await repo.CreateAsync(request.ToModel());
        if (creado.IsSuccess) {
            var f = creado.Value;
            await cassandraRepo.SincronizarFunkoAsync(f.Id, f.Name, f.Stock, f.Precio,
                f.CategoriaId, categoria.Value.Nombre,
                f.ProveedorId, proveedor.Value.Nombre);
        }

        var funko = creado.Value;
        await NotificarFunkoCreado(funko);
        return creado.Map(f => f.ToDto());
    }

    public async Task<Result<FunkoResponseDto, DomainError>> Update(long id, FunkoUpdateRequest request) {
        _log.LogInformation("Actualizando funko {Id}: {@Funko}", id, request);
        
        var validation = await _updateValidation.ValidateAsync(request);
        if (!validation.IsValid) {
            _log.LogWarning("Validación fallida al actualizar funko {Id}: {@Errors}", id, validation.Errors);
            return Result.Failure<FunkoResponseDto, DomainError>(
                new ValidationError(validation.Errors[0].ErrorMessage));
        }
        var funkoExistente = await repo.GetByIdAsync(id);
        if (funkoExistente.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(funkoExistente.Error);

        if (funkoExistente.Value.IsDeleted)
            return Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("No se puede actualizar un funko eliminado"));

        var categoria = await repoCategoria.GetById(request.CategoriaId);
        if (categoria.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("La categoría especificada no existe o está eliminada"));

        var proveedor = await repoProveedor.GetById(request.ProveedorId);
        if (proveedor.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(
                new Conflict("El proveedor especificado no existe o está eliminado"));


        var catVieja  = funkoExistente.Value.CategoriaId;
        var provVieja = funkoExistente.Value.ProveedorId;
        var actualizado = await repo.UpdateAsync(id, request.ToModel());
        if (actualizado.IsSuccess) {
            var f = actualizado.Value;
            if (catVieja != f.CategoriaId || provVieja != f.ProveedorId)
                await cassandraRepo.EliminarFunkoAsync(catVieja, provVieja, id);
            await cassandraRepo.SincronizarFunkoAsync(f.Id, f.Name, f.Stock, f.Precio,
                f.CategoriaId, categoria.Value.Nombre,
                f.ProveedorId, proveedor.Value.Nombre);
        }

        return actualizado.Map(f => f.ToDto());
    }

    public async Task<Result<bool, DomainError>> Delete(long id) {
        _log.LogInformation("Eliminando funko: {@Id}", id);
        var funko = await repo.GetByIdAsync(id);
        var borrado = await repo.DeleteAsync(id);
        if (borrado.IsSuccess && funko.IsSuccess) 
            await cassandraRepo.EliminarFunkoAsync(funko.Value.CategoriaId, funko.Value.ProveedorId, id);
        return borrado;
    }

    public Task<Result<string, DomainError>> UploadImagen(IFormFile file) =>
        _storage.SaveFileAsync(file, "funkos");

    public async Task<Result<FunkoResponseDto, DomainError>> UpdateImagen(long id, IFormFile file) {
        var funko = await repo.GetByIdAsync(id);
        if (funko.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(funko.Error);

        var guardada = await _storage.SaveFileAsync(file, "funkos");
        if (guardada.IsFailure)
            return Result.Failure<FunkoResponseDto, DomainError>(guardada.Error);

        var actualizado = await repo.SetImagenAsync(id, guardada.Value);
        if (actualizado.IsFailure) {
            var borrado = await _storage.DeleteFileAsync(guardada.Value);
            if (borrado.IsFailure)
                _log.LogError("No se pudo limpiar la imagen temporal {Imagen}: {Error}",
                    guardada.Value, borrado.Error.Message);
            return Result.Failure<FunkoResponseDto, DomainError>(actualizado.Error);
        }

        if (!string.IsNullOrWhiteSpace(funko.Value.Imagen)) {
            var borrado = await _storage.DeleteFileAsync(funko.Value.Imagen);
            if (borrado.IsFailure)
                _log.LogWarning("La imagen anterior del funko {Id} no se pudo eliminar: {Error}",
                    id, borrado.Error.Message);
        }

        return actualizado.Map(f => f.ToDto());
    }

    private async Task NotificarFunkoCreado(Funko funko) {
        try {
            var notificacion = new {
                FunkoId = funko.Id,
                Nombre = funko.Name,
                Precio = funko.Precio,
                Stock = funko.Stock,
                Imagen = funko.Imagen,
                CategoriaId = funko.CategoriaId,
                ProveedorId = funko.ProveedorId
            };
            await hubContext.Clients
                .Group("admins")
                .SendAsync("FunkoCreado", notificacion);
            _log.LogInformation("Notificacion: Funko creado procediendo a enviar a admins");

        } catch (Exception e) {
            _log.LogError(e, "Error: No se pudo enviar la notificacion");
        }
    }
    
}
