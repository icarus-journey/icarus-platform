using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.DesfazerResgate;

public sealed partial class DesfazerResgateUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    ILogger<DesfazerResgateUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(DesfazerResgateCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        await repositorio.ExecutarAsync(usuarioId, async (usuario, token) =>
        {
            var resgate = await BuscarResgateAsync(repositorio, usuarioId, command.ResgateId, token);
            if (resgate.Estado == EstadoResgate.Estornado) return;
            if (resgate.Estado != EstadoResgate.Resgatado)
                throw Conflito("estado_invalido", "Uma recompensa utilizada não pode ser estornada.");
            resgate.Estado = EstadoResgate.Estornado;
            resgate.EstornadoEm = DateTimeOffset.UtcNow;
            usuario.SaldoPontos += resgate.Custo;
            if (!resgate.Protecao) resgate.Item.Estado = EstadoRecompensa.Disponivel;
            await repositorio.AdicionarMovimentacaoAsync(new MovimentacaoPontos
            {
                UsuarioId = usuarioId,
                ItemId = resgate.ItemId,
                Quantidade = resgate.Custo,
                Tipo = TipoMovimentacaoPontos.EstornoResgate,
                Descricao = $"Estorno: {resgate.Titulo}"
            }, token);
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação DesfazerResgate concluída")]
    private partial void LogOperacaoConcluida();
}
