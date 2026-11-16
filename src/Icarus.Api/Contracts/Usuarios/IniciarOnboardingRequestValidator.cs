using FluentValidation;

namespace Icarus.Api.Contracts.Usuarios;

public sealed class IniciarOnboardingRequestValidator : AbstractValidator<IniciarOnboardingRequest>
{
    public IniciarOnboardingRequestValidator()
    {
        RuleFor(r => r.Respostas)
            .NotEmpty().WithName("Respostas");

        RuleForEach(r => r.Respostas).SetValidator(new RespostaOnboardingRequestValidator());
    }
}

public sealed class RespostaOnboardingRequestValidator : AbstractValidator<RespostaOnboardingRequest>
{
    public RespostaOnboardingRequestValidator()
    {
        RuleFor(r => r.Pergunta)
            .NotEmpty().WithName("Pergunta")
            .MaximumLength(500);

        RuleFor(r => r.Respostas)
            .NotEmpty().WithName("Respostas");

        RuleForEach(r => r.Respostas)
            .NotEmpty().WithName("Resposta")
            .MaximumLength(500);
    }
}
