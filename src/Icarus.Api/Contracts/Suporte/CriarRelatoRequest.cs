using FluentValidation;

namespace Icarus.Api.Contracts.Suporte;

public sealed record CriarRelatoRequest(string ChaveEnvio, string Assunto, string Descricao);

internal sealed class CriarRelatoRequestValidator : AbstractValidator<CriarRelatoRequest>
{
    public CriarRelatoRequestValidator()
    {
        RuleFor(r => r.ChaveEnvio).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(64)
            .Matches("^[a-zA-Z0-9_-]+$");
        RuleFor(r => r.Assunto).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Descricao).NotEmpty().MaximumLength(2000);
    }
}
