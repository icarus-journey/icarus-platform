using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.CriarEntradaDiario;

public sealed partial class CriarEntradaDiarioUseCase(
    IDiarioRepository diario, IUsuarioRepository usuarios, IUnitOfWork unidade,
    ILogger<CriarEntradaDiarioUseCase> logger)
{
    public async Task<EntradaDiarioResult> ExecuteAsync(CriarEntradaDiarioCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado
            || await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken) is null)
            throw new PerfilNaoEncontradoException();

        var entrada = new Diario { UsuarioId = command.UsuarioIdSolicitado, Tipo = command.Tipo,
            DataRegistro = command.DataRegistro, Texto = command.Texto.Trim() };
        await diario.AdicionarAsync(entrada, cancellationToken);
        await unidade.SalvarAlteracoesAsync(cancellationToken);
        LogEntradaCriada(entrada.Id);
        return EntradaDiarioResult.DaEntrada(entrada);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Entrada de diário {EntradaId} criada")]
    private partial void LogEntradaCriada(long entradaId);
}
