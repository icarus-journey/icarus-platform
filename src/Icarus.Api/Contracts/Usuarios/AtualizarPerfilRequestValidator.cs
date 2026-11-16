using FluentValidation;

namespace Icarus.Api.Contracts.Usuarios;

public sealed class AtualizarPerfilRequestValidator : AbstractValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator()
    {
        RuleFor(r => r.CargaHorariaEstudoSemanal).InclusiveBetween(0m, 168m).PrecisionScale(5, 2, true);
        RuleFor(r => r.CargaHorariaEstudoSemanal)
            .Must((r, carga) => carga is null || r.Estuda == true)
            .WithMessage("Informe que estuda para preencher a carga de estudo.");
        RuleFor(r => r.CargaHorariaTrabalhoSemanal).InclusiveBetween(0m, 168m).PrecisionScale(5, 2, true);
        RuleFor(r => r.CargaHorariaTrabalhoSemanal)
            .Must((r, carga) => carga is null || r.Trabalha == true)
            .WithMessage("Informe que trabalha para preencher a carga de trabalho.");
    }
}
