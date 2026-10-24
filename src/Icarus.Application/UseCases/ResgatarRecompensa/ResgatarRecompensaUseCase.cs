using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.ResgatarRecompensa;

public sealed partial class ResgatarRecompensaUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    LojaOptions configuracao,
    ILogger<ResgatarRecompensaUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(ResgatarRecompensaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        await repositorio.ExecutarAsync(usuarioId, async (usuario, token) =>
        {
            if (command.OperacaoId == Guid.Empty)
                throw Conflito("operacao_invalida", "Informe um identificador para o resgate.");
            var resgates = await repositorio.ListarResgatesAsync(usuarioId, token);
            var repetido = resgates.SingleOrDefault(r => r.OperacaoId == command.OperacaoId);
            if (repetido is not null)
            {
                if (repetido.Protecao != (command.RecompensaId is null) || (command.RecompensaId is not null && repetido.ItemId != command.RecompensaId))
                    throw Conflito("operacao_reutilizada", "Este identificador já pertence a outro resgate.");
                return;
            }

            Item item;
            var protecao = command.RecompensaId is null;
            var custo = configuracao.CustoProtecao;
            if (protecao)
            {
                if (resgates.Count(r => r.Protecao && r.Estado == EstadoResgate.Resgatado) >= LimiteProtecoes)
                    throw Conflito("limite_protecoes", "Você já possui três proteções de sequência.");
                item = (await repositorio.ListarItensAsync(usuarioId, token))
                    .SingleOrDefault(i => i.ControlaApp == ControlaApp.Sim)
                    ?? new Item { UsuarioId = usuarioId, Nome = "Proteção de sequência", Valor = custo, ControlaApp = ControlaApp.Sim };
                if (item.Id == 0) await repositorio.AdicionarItemAsync(item, token);
                item.Valor = custo;
            }
            else
            {
                item = await BuscarItemAsync(repositorio, usuarioId, command.RecompensaId!.Value, token);
                if (item.ControlaApp != ControlaApp.Nao || item.Estado != EstadoRecompensa.Disponivel)
                    throw Conflito("estado_invalido", "Somente recompensas disponíveis podem ser resgatadas.");
                custo = item.Valor;
            }
            if (usuario.SaldoPontos < custo)
                throw Conflito("saldo_insuficiente", "Você ainda não possui pontos suficientes para este resgate.");

            var agora = DateTimeOffset.UtcNow;
            usuario.SaldoPontos -= custo;
            item.PrimeiroResgateEm ??= agora;
            if (!protecao) item.Estado = EstadoRecompensa.Resgatada;
            await repositorio.AdicionarResgateAsync(new ResgateItem
            {
                UsuarioId = usuarioId,
                Item = item,
                OperacaoId = command.OperacaoId,
                Titulo = item.Nome,
                Descricao = item.Descricao,
                Custo = custo,
                Protecao = protecao,
                ResgatadoEm = agora
            }, token);
            await repositorio.AdicionarMovimentacaoAsync(new MovimentacaoPontos
            {
                UsuarioId = usuarioId,
                Item = item,
                Quantidade = -custo,
                Tipo = TipoMovimentacaoPontos.CompraItem,
                Descricao = $"Resgate: {item.Nome}"
            }, token);
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação ResgatarRecompensa concluída")]
    private partial void LogOperacaoConcluida();
}
