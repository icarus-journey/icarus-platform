using FluentValidation;
using Icarus.Application.Common;

namespace Icarus.Api.Contracts.Usuarios;

public sealed class AtualizarFusoRequestValidator : AbstractValidator<AtualizarFusoRequest>
{
    public AtualizarFusoRequestValidator()
    {
        RuleFor(r => r.FusoHorario).Must(FusoHorario.EhValido)
            .WithMessage("Informe um fuso IANA válido, como America/Sao_Paulo.");
    }
}
