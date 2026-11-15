using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;

namespace Icarus.Application.UseCases.ConsultarRelatorioSemanal;

public sealed record RelatorioSemanalPublico(long Id, DateTimeOffset GeradoEm, TextoRelatorioSemanal Texto,
    IndicadoresPessoais Indicadores, ConfiguracaoGeracaoRelatorio Geracao,
    ComparacaoSemanal? ComparacaoSemanal = null, string VersaoIndicadores = "1.0.0");
public sealed record ConsultarRelatorioSemanalResult(string Estado, PeriodoAnalitico? Periodo, string Mensagem,
    bool PodeTentarNovamente = false, RelatorioSemanalPublico? Relatorio = null, string VersaoContrato = "1.1.0");
