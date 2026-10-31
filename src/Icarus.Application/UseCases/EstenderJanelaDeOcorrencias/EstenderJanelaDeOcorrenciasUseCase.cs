using Icarus.Application.Common;
using Icarus.Application.Interfaces;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.EstenderJanelaDeOcorrencias;

/// <summary>
/// Rotina de manutenção da janela de ocorrências (RF-04.5, DER seção 12): para cada missão
/// recorrente ativa, garante que exista ocorrência gerada para todo dia aplicável nos próximos
/// 30 dias, sem duplicar as que já existem. Chamada periodicamente por
/// <c>RecorrenciaMaintenanceService</c> (hosted service) — não é disparada pela API
/// diretamente, então não há ownership a checar aqui.
///
/// Também inativa automaticamente (RF-04.6) a missão cuja <see cref="Recorrencia.DataFim"/> já
/// passou — mesma regra do botão manual de parar: cancela pendentes não vencidas (não deveria
/// haver nenhuma, já que a geração parou na data fim) e preserva pendentes já vencidas como
/// histórico, sem permitir concluí-las depois (<c>ConcluirMissaoUseCase</c> rejeita conclusão de
/// missão inativa).
/// </summary>
public sealed partial class EstenderJanelaDeOcorrenciasUseCase(
    IMissaoRepository missaoRepository,
    IOcorrenciaRepository ocorrenciaRepository,
    IUnitOfWork unitOfWork,
    ILogger<EstenderJanelaDeOcorrenciasUseCase> logger)
{
    public async Task<EstenderJanelaDeOcorrenciasResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var missoes = await missaoRepository.ListarRecorrentesAtivasAsync(cancellationToken);
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalCriadas = 0;
        var totalInativadas = 0;

        foreach (var missao in missoes)
        {
            // Inclui também campanhas encerradas antes desta regra existir; não recria planejamento.
            if (missao.Campanha?.Estado is EstadoCampanha.Concluida or EstadoCampanha.Excluida)
            {
                var hojeDoTitular = FusoHorario.DataLocal(DateTimeOffset.UtcNow, missao.Campanha.Epico?.Usuario?.FusoHorario);
                foreach (var pendente in await ocorrenciaRepository.ListarPendentesNaoVencidasAsync(missao.Id, hojeDoTitular, cancellationToken))
                    pendente.Status = StatusOcorrencia.Excluida;
                missao.Status = StatusMissao.Inativa;
                totalInativadas++;
                continue;
            }
            var recorrencia = missao.Recorrencia!;
            var dataBase = recorrencia.AlteracoesAPartirDe is null ? hoje
                : FusoHorario.DataLocal(DateTimeOffset.UtcNow, missao.Usuario?.FusoHorario);
            var fimDaJanela = dataBase.AddDays(GeradorDeDatasRecorrencia.DiasDaJanelaPadrao);

            if (recorrencia.DataFim is { } dataFimVencida && dataFimVencida < dataBase)
            {
                var pendentesNaoVencidas = await ocorrenciaRepository.ListarPendentesNaoVencidasAsync(missao.Id, dataBase, cancellationToken);
                foreach (var ocorrenciaPendente in pendentesNaoVencidas)
                {
                    ocorrenciaPendente.Status = StatusOcorrencia.Excluida;
                }

                missao.Status = StatusMissao.Inativa;
                totalInativadas++;
                continue;
            }

            var tipo = Enum.Parse<TipoRecorrencia>(recorrencia.Tipo, ignoreCase: true);

            var inicioEfetivo = recorrencia.DataInicio > dataBase ? recorrencia.DataInicio : dataBase;
            var fimEfetivo = recorrencia.DataFim is { } dataFim && dataFim < fimDaJanela ? dataFim : fimDaJanela;
            if (recorrencia.AlteracoesAPartirDe is { } alteracoes)
            {
                if (alteracoes > inicioEfetivo) inicioEfetivo = alteracoes;
                if (missao.Campanha is { } campanha)
                {
                    if (campanha.PrazoInicio > inicioEfetivo) inicioEfetivo = campanha.PrazoInicio;
                    if (campanha.PrazoFim < fimEfetivo) fimEfetivo = campanha.PrazoFim;
                }
            }
            if (inicioEfetivo > fimEfetivo)
            {
                continue;
            }

            var datasPossiveis = GeradorDeDatasRecorrencia.Gerar(inicioEfetivo, fimEfetivo, tipo, recorrencia.Regra);
            var datasExistentes = await ocorrenciaRepository.ListarDatasExistentesAsync(missao.Id, inicioEfetivo, fimEfetivo, cancellationToken);
            var datasFaltantes = datasPossiveis.Except(datasExistentes).ToList();

            foreach (var data in datasFaltantes)
            {
                await ocorrenciaRepository.AdicionarAsync(new Ocorrencia { MissaoId = missao.Id, DataPrevista = data, Planejamento = PlanejamentoOcorrencia.Capturar(missao) }, cancellationToken);
            }

            if (datasFaltantes.Count > 0)
            {
                missao.AtualizadoEm = DateTimeOffset.UtcNow;
                if (missao.Campanha is not null) missao.Campanha.AtualizadoEm = DateTimeOffset.UtcNow;
            }
            totalCriadas += datasFaltantes.Count;
        }

        if (totalCriadas > 0 || totalInativadas > 0)
        {
            await unitOfWork.SalvarAlteracoesAsync(cancellationToken);
        }

        LogJanelaEstendida(missoes.Count, totalCriadas, totalInativadas);

        return new EstenderJanelaDeOcorrenciasResult(missoes.Count, totalCriadas, totalInativadas);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Janela de ocorrências mantida: {MissoesProcessadas} missões recorrentes verificadas, {OcorrenciasCriadas} ocorrências novas criadas, {MissoesInativadas} inativada(s) por fim da recorrência")]
    private partial void LogJanelaEstendida(int missoesProcessadas, int ocorrenciasCriadas, int missoesInativadas);
}
