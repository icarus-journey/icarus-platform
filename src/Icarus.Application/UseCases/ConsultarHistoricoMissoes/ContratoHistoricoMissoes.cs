using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using Icarus.Application.Common;
using System.Text.Json;

namespace Icarus.Application.UseCases.ConsultarHistoricoMissoes;

public enum EstadoHistoricoMissao { Todos, Pendente, Atrasada, Feita, FeitaComAtraso, Cancelada }
public sealed record FiltroHistoricoMissoes(DateOnly Inicio, DateOnly Fim, EstadoHistoricoMissao Estado, long? CampanhaId, bool Avulsas, int Limite);
public sealed record CursorHistoricoMissoes(Guid UsuarioId, FiltroHistoricoMissoes Filtro, DateOnly UltimaData, long UltimoId,
    long LimiteId, DateTimeOffset ConsultadoEm, string FusoHorario);
public sealed record CampanhaHistoricoMissao(long Id, string Titulo, bool Removida);
public sealed record RegistroHistoricoMissao(long Id, long MissaoId, string Titulo, bool TituloPreservado, bool MissaoRemovida,
    DateOnly DataPrevista, EstadoHistoricoMissao Estado, CampanhaHistoricoMissao? Campanha,
    DateTimeOffset? RealizadaEm, DateTimeOffset? RegistradaEm, string? FusoHorarioRealizacao, long? PontosRecebidos)
{
    public static RegistroHistoricoMissao DaOcorrencia(Ocorrencia o, DateOnly hoje)
    {
        PlanejamentoOcorrencia? planejamento;
        try { planejamento = PlanejamentoOcorrencia.DaExecucao(o.Missao, o); }
        catch (JsonException) { planejamento = null; }
        if (string.IsNullOrWhiteSpace(planejamento?.Titulo)) planejamento = null;
        var estado = o.Status switch
        {
            StatusOcorrencia.Concluida => EstadoHistoricoMissao.Feita,
            StatusOcorrencia.ConcluidaComAtraso => EstadoHistoricoMissao.FeitaComAtraso,
            StatusOcorrencia.Excluida => EstadoHistoricoMissao.Cancelada,
            _ => o.DataPrevista < hoje ? EstadoHistoricoMissao.Atrasada : EstadoHistoricoMissao.Pendente
        };
        var campanha = o.Missao.Campanha;
        var movimentos = o.MovimentacoesPontos.Where(m => m.UsuarioId == o.Missao.UsuarioId && m.Tipo == TipoMovimentacaoPontos.GanhoMissao).ToArray();
        return new(o.Id, o.MissaoId, planejamento?.Titulo ?? o.Missao.Titulo, planejamento is not null,
            o.Missao.Status == StatusMissao.Excluida, o.DataPrevista, estado,
            campanha is null ? null : new(campanha.Id, campanha.Titulo,
                campanha.Estado == EstadoCampanha.Excluida || campanha.Epico.Estado == EstadoEpico.Excluido),
            o.RealizadaEm, o.ConcluidaEm, FusoHorario.EhValido(o.FusoHorarioRealizacao) ? o.FusoHorarioRealizacao : null,
            movimentos.Length == 0 ? null : movimentos.Sum(m => m.Quantidade));
    }
}
public sealed record PaginaHistoricoMissoes(string VersaoContrato, FiltroHistoricoMissoes Filtro, string FusoHorario,
    DateTimeOffset ConsultadoEm, IReadOnlyList<RegistroHistoricoMissao> Registros, string? ProximoCursor, bool HistoricoParcial, bool TemRegistros);
public sealed record DetalheHistoricoMissao(string VersaoContrato, string FusoHorario, RegistroHistoricoMissao Registro);
public sealed record ConsultaHistoricoMissoes(Guid Autenticado, Guid Titular, DateOnly? Inicio = null, DateOnly? Fim = null,
    EstadoHistoricoMissao Estado = EstadoHistoricoMissao.Todos, long? CampanhaId = null, bool Avulsas = false, int Limite = 20, string? Cursor = null);
public sealed record LoteHistoricoMissoes(IReadOnlyList<RegistroHistoricoMissao> Registros, bool TemMais, long LimiteId, bool HistoricoParcial);
