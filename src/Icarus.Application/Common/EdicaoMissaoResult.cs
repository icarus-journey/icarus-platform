using System.Globalization;
using System.Text.Json;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public sealed record ConfiguracaoRecorrenciaResult(TipoRecorrencia Tipo, IReadOnlyList<DayOfWeek> DiasSemana,
    int? DiaDoMes, DateOnly DataInicio, DateOnly? DataFim, DateOnly? AlteracoesAPartirDe)
{
    public static ConfiguracaoRecorrenciaResult? DaMissao(Missao missao)
    {
        if (missao.Recorrencia is not { } r) return null;
        var tipo = Enum.Parse<TipoRecorrencia>(r.Tipo, true);
        using var regra = JsonDocument.Parse(r.Regra);
        var dias = tipo == TipoRecorrencia.Semanal
            ? regra.RootElement.GetProperty("dias_semana").EnumerateArray().Select(d => Enum.Parse<DayOfWeek>(d.GetString()!, true)).ToArray()
            : [];
        return new(tipo, dias, tipo == TipoRecorrencia.Mensal ? regra.RootElement.GetProperty("dia_mes").GetInt32() : null,
            r.DataInicio, r.DataFim, r.AlteracoesAPartirDe);
    }
}

public sealed record EdicaoMissaoResult(string VersaoContrato, long Id, long? CampanhaId, string Titulo, string? Descricao,
    string? Beneficio, Dificuldade Dificuldade, int Pontuacao, DateOnly? DataLimite, DateTimeOffset Horario,
    StatusMissao Status, DateTimeOffset? ConcluidaEm, bool Recorrente, string Versao, bool PodeEditar,
    ConfiguracaoRecorrenciaResult? ConfiguracaoRecorrencia, ExecucaoMissaoResult Execucao)
{
    public static string VersaoDaMissao(Missao m) => m.AtualizadoEm.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);

    public static EdicaoMissaoResult DaMissao(Missao m, DateTimeOffset? agora = null) => new("1.2.0", m.Id, m.CampanhaId, m.Titulo, m.Descricao,
        m.Beneficio, m.Dificuldade, m.Pontuacao, m.DataLimite, m.Horario, m.Status, m.ConcluidaEm,
        m.Recorrencia is not null, VersaoDaMissao(m), m.Status == StatusMissao.Ativa
            && (m.Recorrencia != null || (m.ConcluidaEm == null && !m.Ocorrencias.Any(o => o.ConcluidaEm != null)))
            && m.Campanha?.Estado is not (EstadoCampanha.Concluida or EstadoCampanha.Excluida),
        ConfiguracaoRecorrenciaResult.DaMissao(m), ExecucaoMissaoResult.DaMissao(m, agora ?? TimeProvider.System.GetUtcNow()));
}
