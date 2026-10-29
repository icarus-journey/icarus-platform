namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar concluir uma missão inativa (RF-04.6: inativação preserva as ocorrências
/// atrasadas como histórico, sem permitir concluí-las).
/// </summary>
public sealed class MissaoInativaException() : Exception("Missão inativa não pode ser concluída.");
