namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar editar uma campanha já concluída, ou atribuir uma missão nova a ela
/// (RF-03.3/RF-03.4: conclusão é um estado final — a campanha não deveria mudar nem ganhar
/// trabalho novo depois disso).
/// </summary>
public sealed class CampanhaConcluidaException() : Exception("Campanha concluída não pode ser alterada.");
