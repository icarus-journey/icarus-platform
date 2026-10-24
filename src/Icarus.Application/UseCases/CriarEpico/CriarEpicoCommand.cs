using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarEpico;

/// <summary>
/// <see cref="PrazoInicio"/> nulo assume hoje; <see cref="PrazoFim"/> nulo assume um ano após
/// o início (mesmo padrão do onboarding por IA). Não existe Pontuacao aqui — ela é sempre
/// calculada a partir de <see cref="Dificuldade"/> pelo caso de uso, nunca recebida do chamador.
/// </summary>
public sealed record CriarEpicoCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    string Titulo,
    string? Descricao,
    AreaDaVida AreaDaVida,
    Dificuldade Dificuldade,
    DateOnly? PrazoInicio,
    DateOnly? PrazoFim);
