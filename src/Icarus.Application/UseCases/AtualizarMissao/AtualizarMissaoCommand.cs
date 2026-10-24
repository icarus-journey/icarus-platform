using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarMissao;

/// <summary>Não inclui CampanhaId (sem re-vínculo via edição) nem Status (ciclo de vida fora de escopo aqui).</summary>
public sealed record AtualizarMissaoCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    long MissaoId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario);
