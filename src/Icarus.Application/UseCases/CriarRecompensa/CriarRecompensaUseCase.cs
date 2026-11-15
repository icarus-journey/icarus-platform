using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;
using static Icarus.Application.Common.RegrasLoja;

namespace Icarus.Application.UseCases.CriarRecompensa;

public sealed partial class CriarRecompensaUseCase(
    ILojaRepository repositorio,
    IUnitOfWork unitOfWork,
    ConsultarLojaUseCase consultarLoja,
    ILogger<CriarRecompensaUseCase> logger)
{
    public async Task<LojaResult> ExecuteAsync(CriarRecompensaCommand command, CancellationToken ct)
    {
        var usuarioId = command.UsuarioIdSolicitado;
        ValidarTitular(command.UsuarioIdAutenticado, usuarioId);
        var dados = new DadosRecompensa(command.Titulo, command.Descricao, command.Custo);
        await repositorio.ExecutarAsync(usuarioId, async (_, token) =>
        {
            ValidarDados(dados);
            await repositorio.AdicionarItemAsync(new Item
            {
                UsuarioId = usuarioId,
                Nome = dados.Titulo.Trim(),
                Descricao = dados.Descricao?.Trim(),
                Valor = dados.Custo
            }, token);
            await unitOfWork.SalvarAlteracoesAsync(token);
        }, ct);

        LogOperacaoConcluida();
        return await consultarLoja.ExecuteAsync(new(command.UsuarioIdAutenticado, usuarioId), ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Operação CriarRecompensa concluída")]
    private partial void LogOperacaoConcluida();
}
