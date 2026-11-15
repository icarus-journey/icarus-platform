using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.AtualizarRecompensa;

public sealed partial class AtualizarRecompensaUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    ILogger<AtualizarRecompensaUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(AtualizarRecompensaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        var dados = new DadosRecompensa(command.Titulo, command.Descricao, command.Custo);
        await repositorio.ExecutarAsync(usuarioId, async (_, token) =>
        {
            ValidarDados(dados);
            var item = await BuscarItemAsync(repositorio, usuarioId, command.RecompensaId, token);
            ValidarManutencao(item);
            if (item.PrimeiroResgateEm is not null)
                throw Conflito("recompensa_imutavel", "Recompensas já resgatadas não podem ser editadas, mesmo após estorno.");
            item.Nome = dados.Titulo.Trim();
            item.Descricao = dados.Descricao?.Trim();
            item.Valor = dados.Custo;
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação AtualizarRecompensa concluída")]
    private partial void LogOperacaoConcluida();
}
