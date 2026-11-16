namespace Icarus.Application.Exceptions;

/// <summary>
/// Lançada ao tentar editar um épico já concluído, ou atribuir uma campanha nova a ele
/// (RF-03.2/RF-03.8: conclusão é um estado final — o épico não deveria mudar nem ganhar
/// trabalho novo depois disso).
/// </summary>
public sealed class EpicoConcluidoException() : Exception("Épico concluído não pode ser alterado.");
