using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.UtilizarRecompensa;

public sealed partial class UtilizarRecompensaUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    ILogger<UtilizarRecompensaUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(UtilizarRecompensaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        await repositorio.ExecutarAsync(usuarioId, async (_, token) =>
        {
            var resgate = await BuscarResgateAsync(repositorio, usuarioId, command.ResgateId, token);
            if (resgate.Protecao)
                throw Conflito("protecao_automatica", "A proteção é consumida automaticamente pela sequência.");
            if (resgate.Estado == EstadoResgate.Utilizado) return;
            if (resgate.Estado != EstadoResgate.Resgatado)
                throw Conflito("estado_invalido", "Somente uma recompensa resgatada pode ser utilizada.");
            resgate.Estado = EstadoResgate.Utilizado;
            resgate.UtilizadoEm = DateTimeOffset.UtcNow;
            resgate.Item.Estado = EstadoRecompensa.Utilizada;
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação UtilizarRecompensa concluída")]
    private partial void LogOperacaoConcluida();
}
