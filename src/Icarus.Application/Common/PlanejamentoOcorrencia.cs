using System.Text.Json;
using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

/// <summary>Configuração preservada para cada execução; não substitui o crédito efetivo do extrato.</summary>
public sealed record PlanejamentoOcorrencia(string Versao, string Titulo, string? Descricao, string? Beneficio,
    Dificuldade Dificuldade, int Pontuacao, DateOnly? DataLimite, TimeOnly Horario,
    string? TipoRecorrencia, string? Regra, DateOnly? DataFimRecorrencia)
{
    public static string Capturar(Missao missao) => JsonSerializer.Serialize(new PlanejamentoOcorrencia(
        "1.0.0", missao.Titulo, missao.Descricao, missao.Beneficio, missao.Dificuldade, missao.Pontuacao,
        missao.DataLimite, TimeOnly.FromDateTime(missao.Horario.DateTime),
        missao.Recorrencia?.Tipo, missao.Recorrencia?.Regra, missao.Recorrencia?.DataFim));

    public static PlanejamentoOcorrencia? Ler(Ocorrencia ocorrencia)
        => ocorrencia.Planejamento is null ? null : JsonSerializer.Deserialize<PlanejamentoOcorrencia>(ocorrencia.Planejamento);

    public static PlanejamentoOcorrencia? DaExecucao(Missao missao, Ocorrencia ocorrencia)
    {
        if (ocorrencia.Planejamento is not null) return Ler(ocorrencia);
        var anterior = Historico(missao.Recorrencia).FirstOrDefault(h => ocorrencia.DataPrevista <= h.VigenteAte);
        return anterior is null ? null : JsonSerializer.Deserialize<PlanejamentoOcorrencia>(anterior.Planejamento);
    }

    // Legado sem snapshot permanece intacto: guarda-se a versão da regra, sem reescrever
    // linhas do passado. Duas edições no mesmo dia preservam a configuração original do dia.
    public static void RegistrarVersaoAnterior(Missao missao, DateOnly hoje)
    {
        var historico = Historico(missao.Recorrencia);
        if (historico.Count == 0 || historico[^1].VigenteAte < hoje)
            historico.Add(new(hoje, Capturar(missao)));
        missao.Recorrencia!.HistoricoPlanejamentos = JsonSerializer.Serialize(historico);
    }

    private static List<VersaoAnterior> Historico(Recorrencia? recorrencia)
        => recorrencia?.HistoricoPlanejamentos is not { } json ? [] : JsonSerializer.Deserialize<List<VersaoAnterior>>(json)!;

    public sealed record VersaoAnterior(DateOnly VigenteAte, string Planejamento);
}
