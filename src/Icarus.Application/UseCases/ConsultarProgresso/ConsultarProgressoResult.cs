using Icarus.Application.Common.Analise;

namespace Icarus.Application.UseCases.ConsultarProgresso;

public sealed record ConsultarProgressoResult(string Estado, PeriodoAnalitico? Periodo,
    IndicadoresPessoais? Indicadores, string SituacaoCenarios, string Mensagem, string VersaoContrato = "1.2.0",
    SerieConclusoes? Evolucao = null, ResumoAdesao? Adesao = null);
