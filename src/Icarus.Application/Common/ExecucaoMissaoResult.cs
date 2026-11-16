using FusoDoUsuario = Icarus.Application.Common.FusoHorario;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public sealed record ExecucaoMissaoResult(long? OcorrenciaId, bool PodeConcluir, bool Concluida, string? CodigoBloqueio,
    DateOnly Hoje, string FusoHorario, DateOnly? DataPrevista, DateOnly? ProximaData,
    DateTimeOffset? ConcluidaEm, bool Atrasada, int? Pontuacao, DateTimeOffset? RealizadaEm = null, string? FusoHorarioRealizacao = null)
{
    public static ExecucaoMissaoResult DaMissao(Missao m, DateTimeOffset agora, string? fuso = null, Ocorrencia? selecionada = null)
    {
        fuso ??= m.Usuario?.FusoHorario;
        var hoje = FusoDoUsuario.DataLocal(agora, fuso);
        selecionada ??= RegrasRecorrencia.Selecionar(m, hoje);
        var proxima = m.Ocorrencias.Where(o => RegrasRecorrencia.Pendente(o) && o.DataPrevista > hoje
            && RegrasRecorrencia.DentroDosLimites(m, o.DataPrevista)).OrderBy(o => o.DataPrevista).Select(o => (DateOnly?)o.DataPrevista).FirstOrDefault();
        if (proxima is null && m.Recorrencia is null && m.DataLimite > hoje && RegrasRecorrencia.DentroDosLimites(m, m.DataLimite.Value))
            proxima = m.DataLimite;
        var codigo = m.Status != StatusMissao.Ativa ? "missao_inativa"
            : m.Campanha?.Estado is EstadoCampanha.Concluida or EstadoCampanha.Excluida ? "campanha_encerrada"
            : m.Campanha is { } c && (c.Estado == EstadoCampanha.Planejada || c.PrazoInicio > hoje) ? "campanha_nao_iniciada"
            : m.Recorrencia is not null && hoje > RegrasRecorrencia.Fim(m) ? "recorrencia_encerrada"
            : m.Recorrencia is null && m.ConcluidaEm is not null && (selecionada is null || !RegrasRecorrencia.Concluida(selecionada)) ? "execucao_ja_concluida"
            : selecionada is not null ? null
            : proxima is not null ? "execucao_futura" : "sem_execucao_pendente";
        var concluida = (m.Recorrencia is null && m.ConcluidaEm is not null) || (selecionada is not null && RegrasRecorrencia.Concluida(selecionada));
        var pontos = selecionada is null ? null : (int?)(selecionada.MovimentacoesPontos.FirstOrDefault(p => p.Tipo == TipoMovimentacaoPontos.GanhoMissao)?.Quantidade
            ?? PlanejamentoOcorrencia.DaExecucao(m, selecionada)?.Pontuacao ?? m.Pontuacao);
        return new(selecionada?.Id > 0 ? selecionada.Id : null, codigo is null && !concluida, concluida, codigo, hoje, FusoDoUsuario.EhValido(fuso) ? fuso! : "UTC",
            selecionada?.DataPrevista, proxima, selecionada?.ConcluidaEm ?? (m.Recorrencia is null ? m.ConcluidaEm : null), selecionada is not null && !concluida && selecionada.DataPrevista < hoje, pontos, selecionada?.RealizadaEm, selecionada?.FusoHorarioRealizacao);
    }
}
