namespace Icarus.Application.Interfaces;

public interface IArmazenamentoDeFotoPerfil
{
    Task<string> SalvarAsync(string chave, Stream conteudo, string contentType, long tamanho, CancellationToken cancellationToken);

    Task<(Stream Conteudo, string ContentType)> ObterAsync(string chave, CancellationToken cancellationToken);
}
