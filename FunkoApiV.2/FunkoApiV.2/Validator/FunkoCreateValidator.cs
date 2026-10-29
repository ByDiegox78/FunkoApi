using FluentValidation;
using FunkoApiV._2.Dto;
using FunkoApiV._2.Infrastructure.Interfaces;

namespace FunkoApiV._2.Validator;

public class FunkoCreateValidator : AbstractValidator<FunkoCreateRequest>, ITransientService {
    public FunkoCreateValidator() {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MinimumLength(2).WithMessage("El nombre debe tener 2 caracteres al menos")
            .MaximumLength(20).WithMessage("El nombre solo puede tener 20 caracteres");

        RuleFor(x => x.Precio)
            .InclusiveBetween(0.01m, 50.0m).WithMessage("El precio debe estar entre 0.01 y 50.0");

        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("El ID de la categoría debe ser mayor que 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo");
    }
}