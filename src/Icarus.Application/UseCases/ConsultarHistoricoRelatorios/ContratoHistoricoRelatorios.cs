using System.Text.Json;
using Icarus.Application.Common;
using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Domain.Entities;

namespace Icarus.Application.UseCases.ConsultarHistoricoRelatorios;

public sealed record PeriodoRelatorioPreservado(DateOnly Inicio, DateOnly Fim, string? FusoHorario);
public sealed record ResumoRelatorioSalvo(long Id, PeriodoRelatorioPreservado Periodo, DateTimeOffset GeradoEm,
    string Estado, string? Titulo, ResumoPlanejamentoRelatorio? Planejamento, int? Concluidas, int? DiasComConclusao);
public sealed record ResumoPlanejamentoRelatorio(string Estado, int? Previstas, int? Concluidas, decimal? TaxaConclusao);
public sealed record DetalheRelatorioSalvo(string VersaoContrato, ResumoRelatorioSalvo Resumo,
    RelatorioSemanalPublico Relatorio, string DisponibilidadeReferencias);
public sealed record PaginaHistoricoRelatorios(string VersaoContrato, DateTimeOffset ConsultadoEm,
    IReadOnlyList<ResumoRelatorioSalvo> Relatorios, string? ProximoCursor);
public sealed record CursorHistoricoRelatorios(Guid UsuarioId, int Limite, DateOnly UltimoInicio,
    long UltimoId, long LimiteId, DateTimeOffset ConsultadoEm);
public sealed record LoteHistoricoRelatorios(IReadOnlyList<ResumoRelatorioSalvo> Relatorios, bool TemMais, long LimiteId);
public sealed record ConsultaHistoricoRelatorios(Guid Autenticado, Guid Titular, int Limite = 20, string? Cursor = null);

public static class ApresentacaoHistoricoRelatorios
{
    private static readonly string[] ContagensObrigatorias = ["diasComConclusao", "quantidadeRegistros"];
    public static SnapshotRelatorioSemanal? Ler(Relatorio r)
    {
        try
        {
            using var documento = JsonDocument.Parse(r.Conteudo);
            if (documento.RootElement.ValueKind != JsonValueKind.Object) return null;
            var indicadores = documento.RootElement.EnumerateObject().FirstOrDefault(p => p.Name.Equals("indicadores", StringComparison.OrdinalIgnoreCase)).Value;
            if (indicadores.ValueKind != JsonValueKind.Object || !ContagensObrigatorias.All(
                campo => indicadores.EnumerateObject().Any(p => p.Name.Equals(campo, StringComparison.OrdinalIgnoreCase)))) return null;
            var s = JsonSerializer.Deserialize<SnapshotRelatorioSemanal>(r.Conteudo, ContratoRelatorioSemanal.Json);
            if (s?.Texto is null || s.Indicadores is null || s.Geracao is null
                || s.Indicadores.DistribuicaoPorArea is null || s.Indicadores.Limitacoes is null
                || string.IsNullOrWhiteSpace(s.Geracao.Provedor) || string.IsNullOrWhiteSpace(s.Geracao.Modelo)
                || s.Indicadores.Concluidas < 0 || s.Indicadores.DiasComConclusao < 0
                || (s.ComparacaoSemanal is not null && (s.ComparacaoSemanal.Semana?.Periodo is null || s.ComparacaoSemanal.SemanasAnteriores is null || s.ComparacaoSemanal.MediaQuatroSemanas is null
                    || s.ComparacaoSemanal.Semana.Periodo.Inicio != r.PeriodoInicio || s.ComparacaoSemanal.Semana.Periodo.Fim != r.PeriodoFim))
                || !ContratoRelatorioSemanal.ConteudoValido(s.Texto) || !ContratoRelatorioSemanal.VersaoSuportada(s.VersaoIndicadores)
                || r.PeriodoFim.DayNumber - r.PeriodoInicio.DayNumber != 6) return null;
            return s;
        }
        catch (Exception erro) when (erro is JsonException or ArgumentException or TimeZoneNotFoundException or InvalidTimeZoneException) { return null; }
    }

    public static ResumoRelatorioSalvo Resumir(Relatorio r, string? fuso) => Resumir(r, fuso, Ler(r));

    private static ResumoRelatorioSalvo Resumir(Relatorio r, string? fuso, SnapshotRelatorioSemanal? s)
    {
        var semana = s?.ComparacaoSemanal?.Semana;
        var fusoPreservado = semana?.Periodo.FusoHorario ?? fuso;
        return new(r.Id, new(r.PeriodoInicio, r.PeriodoFim, FusoHorario.EhValido(fusoPreservado) ? fusoPreservado : null), r.CriadoEm,
            s is null ? "indisponivel" : "disponivel", s?.Texto.Titulo,
            semana is null ? null : new(semana.Estado, semana.Previstas, semana.Concluidas, semana.TaxaConclusao),
            s?.Indicadores.Concluidas, s?.Indicadores.DiasComConclusao);
    }

    public static DetalheRelatorioSalvo? Detalhar(Relatorio r, string? fuso)
    {
        var s = Ler(r);
        return s is null ? null : new("1.0.0", Resumir(r, fuso, s),
            new(r.Id, r.CriadoEm, s.Texto, s.Indicadores, s.Geracao, s.ComparacaoSemanal, s.VersaoIndicadores), "nao_registradas");
    }
}
