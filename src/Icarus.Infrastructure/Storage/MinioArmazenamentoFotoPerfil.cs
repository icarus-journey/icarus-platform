using Icarus.Application.Interfaces;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Icarus.Infrastructure.Storage;

/// <summary>
/// Guarda a foto de perfil como objeto no MinIO (bucket único, uma chave por usuário —
/// enviar uma foto nova sobrescreve o objeto anterior).
/// </summary>
public sealed class MinioArmazenamentoFotoPerfil : IArmazenamentoDeFotoPerfil
{
    private readonly IMinioClient _cliente;
    private readonly string _bucket;

    public MinioArmazenamentoFotoPerfil(IOptions<MinioOptions> options)
    {
        var config = options.Value;
        _bucket = config.Bucket;
        _cliente = new MinioClient()
            .WithEndpoint(config.Endpoint)
            .WithCredentials(config.AccessKey, config.SecretKey)
            .WithSSL(config.UsarSsl)
            .Build();
    }

    public async Task<string> SalvarAsync(string chave, Stream conteudo, string contentType, long tamanho, CancellationToken cancellationToken)
    {
        await GarantirBucketAsync(cancellationToken);

        await _cliente.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(chave)
            .WithStreamData(conteudo)
            .WithObjectSize(tamanho)
            .WithContentType(contentType), cancellationToken);

        return chave;
    }

    public async Task<(Stream Conteudo, string ContentType)> ObterAsync(string chave, CancellationToken cancellationToken)
    {
        var status = await _cliente.StatObjectAsync(new StatObjectArgs()
            .WithBucket(_bucket)
            .WithObject(chave), cancellationToken);

        var destino = new MemoryStream();
        await _cliente.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucket)
            .WithObject(chave)
            .WithCallbackStream(stream => stream.CopyTo(destino)), cancellationToken);

        destino.Position = 0;
        return (destino, status.ContentType);
    }

    private async Task GarantirBucketAsync(CancellationToken cancellationToken)
    {
        var existe = await _cliente.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), cancellationToken);
        if (!existe)
        {
            await _cliente.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), cancellationToken);
        }
    }
}
