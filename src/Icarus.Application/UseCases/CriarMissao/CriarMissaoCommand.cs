using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarMissao;

/// <summary>
/// <see cref="CampanhaId"/> nulo cria uma missão avulsa (RF-03: missão não precisa estar
/// atrelada a campanha ou épico). <see cref="Horario"/> no formato "HH:mm"; nulo assume 09:00
/// na data de <see cref="DataLimite"/> (ou hoje, se também nula) — mesmo padrão do onboarding
/// por IA. Recorrência não é suportada por este caso de uso (RF-04, fora de escopo aqui).
/// </summary>
public sealed record CriarMissaoCommand(
    Guid UsuarioIdAutenticado,
    Guid UsuarioIdSolicitado,
    long? CampanhaId,
    string Titulo,
    string? Descricao,
    string? Beneficio,
    Dificuldade Dificuldade,
    DateOnly? DataLimite,
    string? Horario);
