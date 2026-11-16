namespace Icarus.Application.Exceptions;

/// <summary>Lançada quando o arquivo enviado não é uma imagem aceita ou excede o tamanho máximo.</summary>
public sealed class FotoPerfilInvalidaException(string mensagem) : Exception(mensagem);
