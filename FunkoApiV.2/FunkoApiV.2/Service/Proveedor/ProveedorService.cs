using CSharpFunctionalExtensions;
using FluentValidation;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Dto.Proveedor;
using FunkoApiV._2.Error;
using FunkoApiV._2.Infrastructure.Interfaces;
using FunkoApiV._2.Mapper.Provedor;
using FunkoApiV._2.Repository;

namespace FunkoApiV._2.Service;

public class ProveedorService(
    IProveedorRepository repo,
    IFunkoRepository funkoRepo,
    ILogger<ProveedorService> log,
    IValidator<ProveedorRequestDto> validator) : IProveedorService, IScopedService {
    public async Task<IReadOnlyList<ProveedorResponseDto>> GetAll() {
        log.LogDebug("Obteniendo todos los proveedores");
        var prov = await repo.GetAll();
        return prov.Select(p => p.ToDto()).ToList();
    }

    public async Task<IReadOnlyCollection<ProveedorResponseDto>> GetDeleted() {
        log.LogDebug("Obteniendo todos los proveedores eliminados");
        var deleted = await repo.GetAllDeleted();
        return deleted.Select(t => t.ToDto()).ToList();
    }

    public async Task<Result<ProveedorResponseDto, DomainError>> GetById(string id) {
        log.LogDebug("Obteniendo proveedor con id: {@Id}", id);
        var prov = await repo.GetById(id);
        return prov.Map(p => p.ToDto());
    }

    public async Task<Result<ProveedorResponseDto, DomainError>> Add(ProveedorRequestDto proveedor) {
        log.LogDebug("Añadiendo proveedor: {@Prov}", proveedor);
        var validation = await validator.ValidateAsync(proveedor);
        if (!validation.IsValid) {
            log.LogWarning("Validación fallida al crear proveedor: {@Errors}", validation.Errors);
            return Result.Failure<ProveedorResponseDto, DomainError>(
                new ValidationError(validation.Errors[0].ErrorMessage));
        }
        var creado = await repo.Add(proveedor.ToModel());
        return creado.Map(p => p.ToDto());
    }

    public async Task<Result<ProveedorResponseDto, DomainError>> Update(string id, ProveedorRequestDto proveedor) {
        log.LogDebug("Actualizando proveedor con id: {Id} with: {@Prov}", id, proveedor);

        var validation = await validator.ValidateAsync(proveedor);
        if (!validation.IsValid) {
            log.LogWarning("Validación fallida al actualizar proveedor {Id}: {@Errors}", id, validation.Errors);
            return Result.Failure<ProveedorResponseDto, DomainError>(
                new ValidationError(validation.Errors[0].ErrorMessage));
        }
        log.LogDebug("Realizando actualización del proveedor con id: {@Id}", id);
        return (await repo.Update(id, proveedor.ToModel())).Map(p => p.ToDto());
    }

    public async Task<Result<bool, DomainError>> SoftDelete(string id) {
        log.LogDebug("Eliminando proveedor con id: {@Id}", id);
        var tieneFunkos = await TieneFunkos(id);
        if (tieneFunkos.IsFailure) {
            return tieneFunkos;
        }
        return await repo.SoftDelete(id);
    }

    private async Task<Result<bool, DomainError>> TieneFunkos(string id) {
        var paged = await funkoRepo.GetPagedAsync(
            new FunkoFilterRequest { ProveedorId = id, Page = 0, Size = 1 });
        if (paged.Total > 0)
            return Result.Failure<bool, DomainError>(
                new Conflict("No se puede borrar el proveedor si hay un funko asociado"));
        return Result.Success<bool, DomainError>(true);
    } 
}