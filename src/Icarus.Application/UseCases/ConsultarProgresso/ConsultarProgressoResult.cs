using Icarus.Application.Common.Analise;

namespace Icarus.Application.UseCases.ConsultarProgresso;

public sealed record ConsultarProgressoResult(string Estado, PeriodoAnalitico? Periodo,
    IndicadoresPessoais? Indicadores, string SituacaoCenarios, string Mensagem, string VersaoContrato = "1.4.0",
    SerieConclusoes? Evolucao = null, ResumoAdesao? Adesao = null, PanoramaObjetivos? Objetivos = null, Icarus.Application.Common.Relatorios.ComparacaoSemanal? ComparacaoSemanal = null, int? DiferencaFeitasSemanaAnterior = null);
