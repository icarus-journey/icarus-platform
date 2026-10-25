using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.AtualizarEntradaDiario;

public sealed partial class AtualizarEntradaDiarioUseCase(
    IDiarioRepository diario, IUnitOfWork unidade,
    ILogger<AtualizarEntradaDiarioUseCase> logger)
{
    public async Task<EntradaDiarioResult> ExecuteAsync(AtualizarEntradaDiarioCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
            throw new PerfilNaoEncontradoException();
        var entrada = await diario.BuscarPorIdAsync(command.EntradaId, command.UsuarioIdSolicitado, cancellationToken);
        if (entrada is null || entrada.ExcluidaEm is not null) throw new EntradaDiarioNaoEncontradaException();
        entrada.Tipo = command.Tipo;
        entrada.DataRegistro = command.DataRegistro;
        entrada.Texto = command.Texto.Trim();
        await unidade.SalvarAlteracoesAsync(cancellationToken);
        LogEntradaAtualizada(entrada.Id);
        return EntradaDiarioResult.DaEntrada(entrada);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Entrada de diário {EntradaId} atualizada")]
    private partial void LogEntradaAtualizada(long entradaId);
}
