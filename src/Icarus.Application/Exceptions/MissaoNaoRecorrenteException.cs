namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar inativar uma missão não recorrente (RF-04.6: só missão recorrente pode
/// ser inativada — a não recorrente apenas conclui, no prazo ou com atraso).
/// </summary>
public sealed class MissaoNaoRecorrenteException() : Exception("Somente missão recorrente pode ser inativada.");
