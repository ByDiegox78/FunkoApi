using CSharpFunctionalExtensions;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Repository;

public interface ICategoriaRepository {
    Task<IReadOnlyList<Categoria>> GetAll();

    Task<Result<Categoria, DomainError>> GetById(int id);
}