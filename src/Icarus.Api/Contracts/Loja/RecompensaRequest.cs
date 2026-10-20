using FluentValidation;

namespace Icarus.Api.Contracts.Loja;

public sealed record RecompensaRequest(string Titulo, string? Descricao, int Custo);
public sealed record ResgateRequest(Guid OperacaoId);

public sealed class RecompensaRequestValidator : AbstractValidator<RecompensaRequest>
{
    public RecompensaRequestValidator()
    {
        RuleFor(r => r.Titulo).NotEmpty().MaximumLength(120);
        RuleFor(r => r.Descricao).MaximumLength(1000);
        RuleFor(r => r.Custo).InclusiveBetween(1, 1_000_000);
    }
}

public sealed class ResgateRequestValidator : AbstractValidator<ResgateRequest>
{
    public ResgateRequestValidator() => RuleFor(r => r.OperacaoId).NotEmpty();
}
