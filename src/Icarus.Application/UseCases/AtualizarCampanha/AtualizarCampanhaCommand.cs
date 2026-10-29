using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarCampanha;

/// <summary>Não inclui AreaDaVida (herdada do épico, imutável) nem EpicoId (sem re-vínculo via edição).</summary>
public sealed record AtualizarCampanhaCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    long CampanhaId,
    string Titulo,
    string? Descricao,
    Dificuldade Dificuldade,
    DateOnly PrazoInicio,
    DateOnly PrazoFim,
    decimal? ValorMensurado,
    string? Unidade);
