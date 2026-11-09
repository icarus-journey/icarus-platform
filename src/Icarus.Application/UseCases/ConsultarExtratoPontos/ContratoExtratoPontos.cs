using Icarus.Application.Common;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;
using System.Text.Json;

namespace Icarus.Application.UseCases.ConsultarExtratoPontos;

public enum NaturezaExtrato { Todos, Recebidos, Usados }
public sealed record FiltroExtratoPontos(DateOnly? Inicio, DateOnly? Fim, bool TodoHistorico, NaturezaExtrato Natureza, int Limite);
public sealed record CursorExtratoPontos(Guid UsuarioId, FiltroExtratoPontos Filtro, DateTimeOffset UltimaData,
    long UltimoId, long LimiteId, DateTimeOffset ConsultadoEm, string FusoHorario);
public sealed record ConsultaExtratoPontos(Guid Autenticado, Guid Titular, DateOnly? Inicio = null, DateOnly? Fim = null,
    bool TodoHistorico = false, NaturezaExtrato Natureza = NaturezaExtrato.Todos, int Limite = 20, string? Cursor = null);
public sealed record MovimentoExtratoPontos(long Id, TipoMovimentacaoPontos Tipo, long Quantidade, long? SaldoApos,
    string? TituloOrigem, bool TituloPreservado, bool OrigemRemovida, DateTimeOffset RegistradoEm, DateOnly DataLocal)
{
    public static MovimentoExtratoPontos DoMovimento(MovimentacaoPontos m, string fuso)
    {
        string? titulo = string.IsNullOrWhiteSpace(m.TituloOrigem) ? null : m.TituloOrigem;
        var preservado = titulo is not null;
        var removida = false;
        if (m.Tipo == TipoMovimentacaoPontos.GanhoMissao && m.Ocorrencia?.Missao is { } missao && missao.UsuarioId == m.UsuarioId)
        {
            removida = missao.Status == StatusMissao.Excluida;
            if (titulo is null)
            {
                try { titulo = PlanejamentoOcorrencia.DaExecucao(missao, m.Ocorrencia)?.Titulo; }
                catch (JsonException) { titulo = null; }
                preservado = !string.IsNullOrWhiteSpace(titulo);
                if (!preservado) titulo = missao.Titulo;
            }
        }
        if (m.Tipo is TipoMovimentacaoPontos.CompraItem or TipoMovimentacaoPontos.EstornoResgate
            && m.Item is { } item && item.UsuarioId == m.UsuarioId)
        {
            removida = item.Estado == EstadoRecompensa.Excluida;
            titulo ??= item.Nome;
        }
        return new(m.Id, m.Tipo, m.Quantidade, m.SaldoResultante, string.IsNullOrWhiteSpace(titulo) ? null : titulo,
            preservado, removida, m.CriadoEm, FusoHorario.DataLocal(m.CriadoEm, fuso));
    }
}
public sealed record LoteExtratoPontos(long SaldoAtual, IReadOnlyList<MovimentoExtratoPontos> Movimentos, bool TemMais, long LimiteId);
public sealed record PaginaExtratoPontos(string VersaoContrato, FiltroExtratoPontos Filtro, string FusoHorario,
    DateTimeOffset ConsultadoEm, long SaldoAtual, IReadOnlyList<MovimentoExtratoPontos> Movimentos, string? ProximoCursor, bool TemRegistros);
public sealed record DetalheExtratoPontos(string VersaoContrato, string FusoHorario, MovimentoExtratoPontos Movimento);
