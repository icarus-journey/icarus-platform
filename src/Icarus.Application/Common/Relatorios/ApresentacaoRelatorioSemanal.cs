using System.Text.Json;
using Icarus.Application.Common.Analise;
using Icarus.Application.UseCases.ConsultarRelatorioSemanal;
using Icarus.Domain.Entities;

namespace Icarus.Application.Common.Relatorios;

public static class ApresentacaoRelatorioSemanal
{
    public static ConsultarRelatorioSemanalResult Criar(PeriodoAnalitico periodo, Relatorio? relatorio,
        TrabalhoRelatorioSemanal? trabalho, DateTimeOffset agora)
    {
        if (relatorio is not null)
        {
            var conteudo = JsonSerializer.Deserialize<SnapshotRelatorioSemanal>(relatorio.Conteudo, ContratoRelatorioSemanal.Json)!;
            var periodoSalvo = trabalho is null ? periodo : new PeriodoAnalitico(trabalho.PeriodoInicio, trabalho.PeriodoFim, trabalho.FusoHorario);
            return new("disponivel", periodoSalvo, "Uma síntese por IA dos registros disponíveis; você pode ignorar a reflexão.",
                Relatorio: new(relatorio.Id, relatorio.CriadoEm, conteudo.Texto, conteudo.Indicadores, conteudo.Geracao));
        }
        if (trabalho is null) return new("ausente", periodo, "Seu relatório da semana ainda não foi solicitado.");
        if (trabalho.Estado == "processando" && trabalho.ExpiraEm > agora)
            return new("processando", periodo, "Sua semana está sendo preparada. Você pode sair desta tela e voltar depois.");
        var podeTentar = trabalho.Tentativas < ContratoRelatorioSemanal.MaxTentativas;
        return new("falha", periodo, podeTentar
            ? "Não foi possível preparar seu relatório agora. Você pode tentar novamente."
            : "O relatório está temporariamente indisponível. O limite de tentativas desta semana foi atingido.", podeTentar);
    }
}
