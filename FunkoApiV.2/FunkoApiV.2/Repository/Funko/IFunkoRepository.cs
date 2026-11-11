using CSharpFunctionalExtensions;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Repository;

public interface IFunkoRepository {
    Task<(IReadOnlyList<Funko> Items, int Total)> GetPagedAsync(FunkoFilterRequest filter);

    Task<Result<Funko, DomainError>> GetByIdAsync(long id);

    Task<Result<Funko, DomainError>> CreateAsync(Funko funko);

    Task<Result<Funko, DomainError>> UpdateAsync(long id, Funko funko);

    Task<Result<Funko, DomainError>> SetImagenAsync(long id, string imagen);

    Task<Result<bool, DomainError>> DeleteAsync(long id);
}
