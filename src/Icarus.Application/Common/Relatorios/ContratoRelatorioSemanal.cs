using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Icarus.Application.Common.Analise;

namespace Icarus.Application.Common.Relatorios;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IndicadoresRelatorioSemanal(int? Sequencia, long? PontosGanhos, long? PontosUtilizados,
    int? Previstas, int? Concluidas, int? Pendentes, int? Atrasadas, int? ConcluidasComAtraso, decimal? TaxaConclusao,
    IReadOnlyList<ConclusoesPorArea> DistribuicaoPorArea, int? MaiorAtividadeDeCampanha, decimal? ProgressoManualCampanhas,
    int? RecompensasResgatadas, int? RecompensasUtilizadas, bool HistoricoInsuficienteComparacao)
{
    public static IndicadoresRelatorioSemanal DosIndicadores(IndicadoresPessoais i) => new(i.Sequencia,
        i.PontosGanhos, i.PontosUtilizados, i.Previstas, i.Concluidas, i.Pendentes, i.Atrasadas, i.ConcluidasComAtraso,
        i.TaxaConclusao, i.DistribuicaoPorArea, i.MaiorAtividadeDeCampanha, i.ProgressoManualCampanhas,
        i.RecompensasResgatadas, i.RecompensasUtilizadas, i.HistoricoInsuficienteComparacao);
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SolicitacaoRelatorioSemanal(Guid IdMensagem, Guid CorrelacaoId, Guid? CausalidadeId,
    string Tipo, string VersaoContrato, DateTimeOffset CriadaEm, DateTimeOffset ExpiraEm,
    string ChaveIdempotencia, PeriodoAnalitico Periodo, IndicadoresRelatorioSemanal Indicadores);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TextoRelatorioSemanal(string Titulo, string Resumo, string ProximoAjuste, string Reflexao);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfiguracaoGeracaoRelatorio(string Provedor, string Modelo);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResultadoRelatorioSemanal(Guid IdMensagem, Guid CorrelacaoId, Guid CausalidadeId,
    string Tipo, string VersaoContrato, DateTimeOffset CriadaEm, DateTimeOffset ExpiraEm,
    string ChaveIdempotencia, PeriodoAnalitico Periodo, string Estado,
    TextoRelatorioSemanal? Conteudo, string? FalhaCodigo, ConfiguracaoGeracaoRelatorio? Geracao);
public sealed record SnapshotRelatorioSemanal(TextoRelatorioSemanal Texto, IndicadoresPessoais Indicadores,
    ConfiguracaoGeracaoRelatorio Geracao);

public static partial class ContratoRelatorioSemanal
{
    public const string Versao = "1.0.0";
    public const int MaxTentativas = 3;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static bool ConteudoValido(TextoRelatorioSemanal? texto) => texto is not null
        && CampoValido(texto.Titulo, 100) && CampoValido(texto.Resumo, 800)
        && CampoValido(texto.ProximoAjuste, 600) && CampoValido(texto.Reflexao, 400)
        && !ExpressaoClinica().IsMatch(string.Join(' ', texto.Titulo, texto.Resumo, texto.ProximoAjuste, texto.Reflexao));
    private static bool CampoValido(string? valor, int limite) => !string.IsNullOrWhiteSpace(valor) && valor.Length <= limite;
    [GeneratedRegex(@"diagn[oó]stic|garant(?:e|ia|ido)|probabilidade|cura(?:r|do|da|s)?\b|redu[zç].{0,20}risco", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressaoClinica();
}
