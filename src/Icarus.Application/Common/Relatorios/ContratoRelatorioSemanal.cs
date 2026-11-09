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
    string ChaveIdempotencia, PeriodoAnalitico Periodo, IndicadoresRelatorioSemanal Indicadores,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ComparacaoSemanal? ComparacaoSemanal = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TextoRelatorioSemanal(string Titulo, string Resumo, string? ProximoAjuste, string Reflexao);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfiguracaoGeracaoRelatorio(string Provedor, string Modelo);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResultadoRelatorioSemanal(Guid IdMensagem, Guid CorrelacaoId, Guid CausalidadeId,
    string Tipo, string VersaoContrato, DateTimeOffset CriadaEm, DateTimeOffset ExpiraEm,
    string ChaveIdempotencia, PeriodoAnalitico Periodo, string Estado,
    TextoRelatorioSemanal? Conteudo, string? FalhaCodigo, ConfiguracaoGeracaoRelatorio? Geracao);
public sealed record SnapshotRelatorioSemanal(TextoRelatorioSemanal Texto, IndicadoresPessoais Indicadores,
    ConfiguracaoGeracaoRelatorio Geracao, ComparacaoSemanal? ComparacaoSemanal = null, string VersaoIndicadores = "1.0.0");

public static partial class ContratoRelatorioSemanal
{
    public const string Versao = "1.2.0";
    public static bool VersaoSuportada(string versao) => versao is "1.0.0" or "1.1.0" or Versao;
    public static (IndicadoresPessoais Indicadores, ComparacaoSemanal? Comparacao, string Versao) LerIndicadores(string carga)
    {
        using var documento = JsonDocument.Parse(carga);
        if (!documento.RootElement.TryGetProperty("indicadores", out _))
            return (JsonSerializer.Deserialize<IndicadoresPessoais>(carga, Json)!, null, "1.0.0");
        var dados = JsonSerializer.Deserialize<DadosRelatorioSemanal>(carga, Json)!;
        if (!VersaoSuportada(dados.VersaoContrato)) throw new JsonException("Versão de indicadores incompatível.");
        return (dados.Indicadores, dados.ComparacaoSemanal, dados.VersaoContrato);
    }
    public const int MaxTentativas = 3;
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static bool ConteudoValido(TextoRelatorioSemanal? texto, string versao = "1.1.0") => texto is not null
        && CampoValido(texto.Titulo, 100) && CampoValido(texto.Resumo, 800)
        && ((versao == "1.2.0" && texto.ProximoAjuste is null) || CampoValido(texto.ProximoAjuste, 600)) && CampoValido(texto.Reflexao, 400)
        && (versao != "1.2.0" || !ExpressaoJulgamento().IsMatch(string.Join(' ', texto.Titulo, texto.Resumo, texto.ProximoAjuste, texto.Reflexao)))
        && !ExpressaoClinica().IsMatch(string.Join(' ', texto.Titulo, texto.Resumo, texto.ProximoAjuste, texto.Reflexao));
    public static bool NumerosSustentados(TextoRelatorioSemanal texto, IndicadoresPessoais indicadores, ComparacaoSemanal? comparacao)
    {
        using var documento = JsonDocument.Parse(JsonSerializer.Serialize(new {
            indicadores = IndicadoresRelatorioSemanal.DosIndicadores(indicadores), comparacao }, Json));
        var permitidos = new HashSet<decimal>();
        void Coletar(JsonElement elemento)
        {
            if (elemento.ValueKind == JsonValueKind.Number && elemento.TryGetDecimal(out var numero)) permitidos.Add(numero);
            else if (elemento.ValueKind == JsonValueKind.Array) foreach (var item in elemento.EnumerateArray()) Coletar(item);
            else if (elemento.ValueKind == JsonValueKind.Object) foreach (var item in elemento.EnumerateObject()) Coletar(item.Value);
        }
        Coletar(documento.RootElement);
        return NumerosNoTexto().Matches(string.Join(' ', texto.Titulo, texto.Resumo, texto.ProximoAjuste, texto.Reflexao))
            .All(m => decimal.TryParse(m.Value.Replace(',', '.'), System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var numero) && permitidos.Contains(numero));
    }
    [GeneratedRegex(@"(?<!\w)\d+(?:[,.]\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumerosNoTexto();
    private static bool CampoValido(string? valor, int limite) => !string.IsNullOrWhiteSpace(valor) && valor.Length <= limite;
    [GeneratedRegex(@"diagn[oó]stic|garant(?:e|ia|ido)|probabilidade|cura(?:r|do|da|s)?\b|redu[zç].{0,20}risco", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressaoClinica();
    [GeneratedRegex(@"sobrecarg|psicol[oó]g|produtiv|dedic|pregui[çc]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpressaoJulgamento();
}
