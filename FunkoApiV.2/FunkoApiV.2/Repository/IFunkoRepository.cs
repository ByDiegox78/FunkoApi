using System.Collections.Generic;
using CSharpFunctionalExtensions;
using FunkoApiV._2.Error;
using FunkoApiV._2.Models;

namespace FunkoApiV._2.Repository;

public interface IFunkoRepository {
    Task<IReadOnlyList<Funko>> GetAll();

    Task<Result<Funko, DomainError>> GetById(int id);

    Task<IReadOnlyList<Funko>> GetAllDeleted();
    
    Task<Result<Funko, DomainError>> Add(Funko funko);

    Task<Result<Funko, DomainError>> Update(int id, Funko funko);
    
    Task<Result<bool, DomainError>> SoftDelete(int id);
    
    Task<Result<bool, DomainError>> Restore(int id);
}