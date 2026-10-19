using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Icarus.Infrastructure.Messaging;

/// <summary>
/// Mantém uma única conexão compartilhada com o RabbitMQ (com recuperação automática).
/// Publisher e consumer abrem seus próprios canais a partir dela — padrão recomendado pelo
/// RabbitMQ: uma conexão, vários canais, nunca o contrário.
/// </summary>
public sealed class RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly RabbitMqOptions options = options.Value;
    private IConnection? conexao;
    private readonly SemaphoreSlim trava = new(1, 1);

    public async Task<IConnection> ObterConexaoAsync(CancellationToken cancellationToken)
    {
        if (conexao is { IsOpen: true })
        {
            return conexao;
        }

        await trava.WaitAsync(cancellationToken);
        try
        {
            if (conexao is { IsOpen: true })
            {
                return conexao;
            }

            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Porta,
                UserName = options.Usuario,
                Password = options.Senha,
                AutomaticRecoveryEnabled = true
            };

            conexao = await factory.CreateConnectionAsync(cancellationToken);
            return conexao;
        }
        finally
        {
            trava.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (conexao is not null)
        {
            await conexao.CloseAsync();
            conexao.Dispose();
        }

        trava.Dispose();
    }
}
