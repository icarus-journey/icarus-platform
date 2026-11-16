using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ExcluirEntradaDiario;

public sealed partial class ExcluirEntradaDiarioUseCase(
    IDiarioRepository diario, IUnitOfWork unidade, TimeProvider relogio,
    ILogger<ExcluirEntradaDiarioUseCase> logger)
{
    public async Task ExecuteAsync(ExcluirEntradaDiarioCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
            throw new PerfilNaoEncontradoException();
        var entrada = await diario.BuscarPorIdAsync(command.EntradaId, command.UsuarioIdSolicitado, cancellationToken)
            ?? throw new EntradaDiarioNaoEncontradaException();
        if (entrada.ExcluidaEm is not null) return;
        entrada.ExcluidaEm = relogio.GetUtcNow();
        await unidade.SalvarAlteracoesAsync(cancellationToken);
        LogEntradaExcluida(entrada.Id);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Entrada de diário {EntradaId} excluída logicamente")]
    private partial void LogEntradaExcluida(long entradaId);
}
