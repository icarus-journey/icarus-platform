using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.ExcluirRecompensa;

public sealed partial class ExcluirRecompensaUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    ILogger<ExcluirRecompensaUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(ExcluirRecompensaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        await repositorio.ExecutarAsync(usuarioId, async (_, token) =>
        {
            var item = await BuscarItemAsync(repositorio, usuarioId, command.RecompensaId, token);
            ValidarManutencao(item);
            item.Estado = EstadoRecompensa.Excluida;
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação ExcluirRecompensa concluída")]
    private partial void LogOperacaoConcluida();
}
