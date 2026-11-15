using FluentValidation;

namespace Icarus.Api.Contracts.Campanhas;

public sealed class RegistrarProgressoCampanhaRequestValidator : AbstractValidator<RegistrarProgressoCampanhaRequest>
{
    public RegistrarProgressoCampanhaRequestValidator()
    {
        RuleFor(r => r.ValorAtual).NotNull().InclusiveBetween(0, 9999999999.99m).PrecisionScale(12, 2, true);
        RuleFor(r => r.VersaoEsperada).NotEmpty().MaximumLength(60);
        RuleFor(r => r.OperacaoId).NotEmpty();
    }
}
