namespace Icarus.Application.Exceptions;

/// <summary>Lançada ao buscar a foto de perfil de um usuário que ainda não tem nenhuma.</summary>
public sealed class FotoPerfilNaoEncontradaException() : Exception("Usuário não tem foto de perfil.");
