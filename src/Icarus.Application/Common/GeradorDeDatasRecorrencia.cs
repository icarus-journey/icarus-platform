using System.Text.Json;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

/// <summary>
/// Converte uma <see cref="Icarus.Domain.Entities.Recorrencia"/> (tipo + regra JSONB) num
/// conjunto de datas concretas dentro de um intervalo — usado tanto na criação da missão
/// quanto na manutenção diária da janela (DER seção 12, RF-04.4/RF-04.5).
/// </summary>
public static class GeradorDeDatasRecorrencia
{
    /// <summary>Tamanho padrão da janela de ocorrências geradas com antecedência (RF-04.5).</summary>
    public const int DiasDaJanelaPadrao = 30;

    public static string ConstruirRegra(TipoRecorrencia tipo, IReadOnlyCollection<DayOfWeek>? diasSemana, int diaMes) => tipo switch
    {
        TipoRecorrencia.Semanal => JsonSerializer.Serialize(new
        {
            dias_semana = diasSemana!.Select(d => d.ToString().ToUpperInvariant()).ToList()
        }),
        TipoRecorrencia.Mensal => JsonSerializer.Serialize(new { dia_mes = diaMes }),
        _ => "{}"
    };

    /// <summary>Gera as datas de <paramref name="inicio"/> a <paramref name="fim"/> (inclusive) que atendem a regra.</summary>
    public static List<DateOnly> Gerar(DateOnly inicio, DateOnly fim, TipoRecorrencia tipo, string regraJson)
    {
        var datas = new List<DateOnly>();
        if (inicio > fim)
        {
            return datas;
        }

        switch (tipo)
        {
            case TipoRecorrencia.Diaria:
                for (var data = inicio; data <= fim; data = data.AddDays(1))
                {
                    datas.Add(data);
                }

                break;

            case TipoRecorrencia.Semanal:
                var diasSemana = ExtrairDiasSemana(regraJson);
                for (var data = inicio; data <= fim; data = data.AddDays(1))
                {
                    if (diasSemana.Contains(data.DayOfWeek))
                    {
                        datas.Add(data);
                    }
                }

                break;

            case TipoRecorrencia.Mensal:
                var diaMes = ExtrairDiaMes(regraJson);
                for (var data = inicio; data <= fim; data = data.AddDays(1))
                {
                    // RF-04.4: "mês curto usa seu último dia" — ex.: dia_mes=31 em fevereiro vira 28/29.
                    var ultimoDiaDoMes = DateTime.DaysInMonth(data.Year, data.Month);
                    var diaEfetivo = Math.Min(diaMes, ultimoDiaDoMes);
                    if (data.Day == diaEfetivo)
                    {
                        datas.Add(data);
                    }
                }

                break;
        }

        return datas;
    }

    private static HashSet<DayOfWeek> ExtrairDiasSemana(string regraJson)
    {
        using var documento = JsonDocument.Parse(regraJson);
        var valores = documento.RootElement.GetProperty("dias_semana");
        return valores.EnumerateArray()
            .Select(v => Enum.Parse<DayOfWeek>(v.GetString()!, ignoreCase: true))
            .ToHashSet();
    }

    private static int ExtrairDiaMes(string regraJson)
    {
        using var documento = JsonDocument.Parse(regraJson);
        return documento.RootElement.GetProperty("dia_mes").GetInt32();
    }
}
