using Icarus.Application.Common;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.RegistrarProgressoCampanha;

public sealed partial class RegistrarProgressoCampanhaUseCase(ICampanhaRepository campanhas,
    IMedicaoCampanhaRepository medicoes, IUsuarioRepository usuarios, IUnitOfWork unidade,
    TimeProvider relogio, ILogger<RegistrarProgressoCampanhaUseCase> logger)
{
    public async Task<ProgressoCampanhaResult> ExecuteAsync(RegistrarProgressoCampanhaCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new CampanhaNaoEncontradaException();
        var campanha = await campanhas.BuscarPorIdAsync(command.CampanhaId, command.UsuarioIdSolicitado, ct)
            ?? throw new CampanhaNaoEncontradaException();
        if (campanha.Estado == EstadoCampanha.Excluida) throw new CampanhaNaoEncontradaException();
        var anterior = await medicoes.BuscarOperacaoAsync(campanha.Id, command.UsuarioIdSolicitado, command.OperacaoId, ct);
        if (anterior is not null)
        {
            if (anterior.ValorAtual != command.ValorAtual)
                throw new ProgressoCampanhaException("operacao_reutilizada", "Esta tentativa já foi usada para outro registro. Atualize o histórico.");
            return ProgressoCampanhaResult.DaCampanha(campanha, await medicoes.ListarAsync(campanha.Id, command.UsuarioIdSolicitado, ct));
        }
        if (campanha.Estado is not (EstadoCampanha.Planejada or EstadoCampanha.EmAndamento))
            throw new ProgressoCampanhaException("campanha_encerrada", "Esta campanha não aceita novos registros de progresso.");
        if (campanha.ValorMensurado is not > 0 || string.IsNullOrWhiteSpace(campanha.Unidade))
            throw new ProgressoCampanhaException("meta_nao_definida", "Defina a quantidade e a unidade da meta antes de registrar seu progresso.");
        if (command.ValorAtual < 0 || command.ValorAtual > 9999999999.99m || decimal.Round(command.ValorAtual, 2) != command.ValorAtual)
            throw new ProgressoCampanhaException("valor_invalido", "Informe um valor válido com até duas casas decimais.");
        if (command.VersaoEsperada != ProgressoCampanhaResult.VersaoDaCampanha(campanha))
            throw new DadosAtualizadosException(new InvalidOperationException("Versão da campanha divergente."));
        var usuario = await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, ct) ?? throw new CampanhaNaoEncontradaException();
        await medicoes.AdicionarAsync(new MedicaoCampanha
        {
            CampanhaId = campanha.Id, ValorAnterior = campanha.ValorAtual, ValorAtual = command.ValorAtual,
            ValorMensurado = campanha.ValorMensurado.Value, Unidade = campanha.Unidade,
            RegistradaEm = relogio.GetUtcNow(), FusoHorario = usuario.FusoHorario, OperacaoId = command.OperacaoId
        }, ct);
        campanha.ValorAtual = command.ValorAtual;
        // Mesmo valor também é uma medição: força a versão a participar da gravação atômica.
        campanha.AtualizadoEm = relogio.GetUtcNow();
        await unidade.SalvarAlteracoesAsync(ct);
        LogProgressoRegistrado(campanha.Id);
        return ProgressoCampanhaResult.DaCampanha(campanha, await medicoes.ListarAsync(campanha.Id, command.UsuarioIdSolicitado, ct));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Progresso da campanha {CampanhaId} registrado")]
    private partial void LogProgressoRegistrado(long campanhaId);
}
