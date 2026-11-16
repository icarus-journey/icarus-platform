using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;

/// <summary>Mantém a janela no dia civil de cada titular, sem alterar passado ou conclusões.</summary>
public sealed partial class EstenderJanelaDeOcorrenciasUseCase(
    IMissaoRepository missaoRepository, IOcorrenciaRepository ocorrenciaRepository,
    IUnitOfWork unitOfWork, ILogger<EstenderJanelaDeOcorrenciasUseCase> logger, TimeProvider? relogio = null)
{
    public async Task<EstenderJanelaDeOcorrenciasResult> ExecuteAsync(CancellationToken ct)
    {
        var missoes = await missaoRepository.ListarRecorrentesAtivasAsync(ct);
        var agora = (relogio ?? TimeProvider.System).GetUtcNow();
        var criadas = 0; var inativadas = 0; var canceladas = 0; var reativadas = 0;
        foreach (var missao in missoes)
        {
            var hoje = FusoHorario.DataLocal(agora, missao.Usuario?.FusoHorario ?? missao.Campanha?.Epico?.Usuario?.FusoHorario);
            if (RegrasRecorrencia.EncerrarSeNecessario(missao, hoje))
            {
                // O repositório mantém a mesma elegibilidade para todos os fluxos de encerramento.
                foreach (var o in await ocorrenciaRepository.ListarPendentesNaoVencidasAsync(missao.Id, hoje, ct))
                    o.Status = StatusOcorrencia.Excluida;
                inativadas++;
                continue;
            }
            var alterou = false;
            foreach (var o in missao.Ocorrencias.Where(o => RegrasRecorrencia.Pendente(o) && o.DataPrevista >= hoje
                && !RegrasRecorrencia.DentroDosLimites(missao, o.DataPrevista)))
            {
                o.Status = StatusOcorrencia.Excluida; canceladas++; alterou = true;
            }
            var (inicio, fim) = RegrasRecorrencia.Janela(missao, hoje);
            var regra = missao.Recorrencia!;
            var datas = GeradorDeDatasRecorrencia.Gerar(inicio, fim, Enum.Parse<TipoRecorrencia>(regra.Tipo, true), regra.Regra);
            var existentes = await ocorrenciaRepository.ListarDatasExistentesAsync(missao.Id, inicio, fim, ct);
            // A missão permanece ativa: uma mudança de prazo planejado pode tornar uma data
            // futura elegível novamente. Não reativa cancelamentos de missões paradas/excluídas.
            foreach (var o in missao.Ocorrencias.Where(o => o.Status == StatusOcorrencia.Excluida && !RegrasRecorrencia.Concluida(o)
                && o.DataPrevista > hoje && datas.Contains(o.DataPrevista)))
            {
                o.Status = StatusOcorrencia.Pendente;
                o.Planejamento = PlanejamentoOcorrencia.Capturar(missao);
                reativadas++; alterou = true;
            }
            foreach (var data in datas.Except(existentes))
            {
                await ocorrenciaRepository.AdicionarAsync(new Ocorrencia
                {
                    MissaoId = missao.Id,
                    DataPrevista = data,
                    Planejamento = PlanejamentoOcorrencia.Capturar(missao)
                }, ct);
                criadas++; alterou = true;
            }
            if (alterou)
            {
                missao.AtualizadoEm = agora;
                if (missao.Campanha is not null) missao.Campanha.AtualizadoEm = agora;
            }
        }
        if (criadas + inativadas + canceladas + reativadas > 0) await unitOfWork.SalvarAlteracoesAsync(ct);
        LogJanelaEstendida(missoes.Count, criadas, inativadas);
        return new(missoes.Count, criadas, inativadas);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Janela mantida: {Missoes} missões, {Criadas} execuções criadas, {Inativadas} missões inativadas")]
    private partial void LogJanelaEstendida(int missoes, int criadas, int inativadas);
}
