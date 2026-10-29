using System.Collections.Generic;
using CSharpFunctionalExtensions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Service;

public interface IFunkoService {
    Task<IReadOnlyList<FunkoResponseDto>> GetAll();
    Task<IReadOnlyCollection<FunkoResponseDto>> GetDeleted();
    Task<Result<FunkoResponseDto, DomainError>> GetById(int id);
    Task<Result<FunkoResponseDto, DomainError>> Add(FunkoCreateRequest funko);
    Task<Result<FunkoResponseDto, DomainError>> Update(int id, FunkoUpdateRequest funko);
    Task<Result<bool, DomainError>> SoftDelete(int id);
    Task<Result<bool, DomainError>> Restore(int id);
    Task<IReadOnlyList<CategoriaDto>> GetAllCategories();
    Task<Result<CategoriaDto, DomainError>> GetByIdCategories(int id);
}