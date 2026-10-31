using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarMissao;

/// <summary>
/// <see cref="Tipo"/> presente diária/semanal/mensal (RF-04.4); <see cref="DiasSemana"/> é
/// obrigatório e não vazio só para <see cref="TipoRecorrencia.Semanal"/>. Mensal usa o dia do
/// mês da referência (<see cref="CriarMissaoCommand.DataLimite"/> ou hoje) — "mês curto usa seu
/// último dia" é tratado na geração, não aqui. <see cref="DataFim"/> nula repete
/// indefinidamente (até parar manualmente, RF-04.6); preenchida limita a geração de novas
/// ocorrências a essa data — depois dela, a missão é inativada, preservando o histórico.
/// </summary>
public sealed record RecorrenciaCommandInfo(TipoRecorrencia Tipo, List<DayOfWeek>? DiasSemana, DateOnly? DataFim);

/// <summary>
/// <see cref="CampanhaId"/> nulo cria uma missão avulsa (RF-03: missão não precisa estar
/// atrelada a campanha ou épico). <see cref="Horario"/> no formato "HH:mm"; nulo assume 09:00
/// na data de <see cref="DataLimite"/> (ou hoje, se também nula) — mesmo padrão do onboarding
/// por IA. <see cref="Recorrencia"/> nula cria missão não recorrente (uma única ocorrência);
/// preenchida gera a janela dos próximos 30 dias (RF-04.5).
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
    string? Horario,
    RecorrenciaCommandInfo? Recorrencia);
