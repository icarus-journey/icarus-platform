using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarEpico;

/// <summary>
/// Não inclui AreaDaVida: a área da vida do épico é imutável após criado (RF-03.2) —
/// Campanha/Missão a herdam e isso quebraria se o épico mudasse de área depois.
/// </summary>
public sealed record AtualizarEpicoCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    long EpicoId,
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly PrazoFim);
