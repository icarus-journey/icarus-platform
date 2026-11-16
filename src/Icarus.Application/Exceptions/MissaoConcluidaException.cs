namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar editar uma missão já concluída (RF-04.3: missão concluída é imutável —
/// ocorrência concluída e pontos recebidos não são reescritos).
/// </summary>
public sealed class MissaoConcluidaException() : Exception("Missão concluída não pode ser editada.");
