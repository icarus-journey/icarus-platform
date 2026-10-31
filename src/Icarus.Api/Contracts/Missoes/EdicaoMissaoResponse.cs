using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

public sealed record EdicaoMissaoResponse(string VersaoContrato, long Id, long? CampanhaId, string Titulo,
    string? Descricao, string? Beneficio, Dificuldade Dificuldade, int Pontuacao, DateOnly? DataLimite,
    DateTimeOffset Horario, StatusMissao Status, DateTimeOffset? ConcluidaEm, bool Recorrente,
    string Versao, bool PodeEditar, ConfiguracaoRecorrenciaResponse? ConfiguracaoRecorrencia, ExecucaoMissaoResponse Execucao)
{
    public static EdicaoMissaoResponse DoResultado(EdicaoMissaoResult r) => new(r.VersaoContrato, r.Id,
        r.CampanhaId, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite,
        r.Horario, r.Status, r.ConcluidaEm, r.Recorrente, r.Versao, r.PodeEditar,
        r.ConfiguracaoRecorrencia is not { } c ? null : new(c.Tipo, c.DiasSemana, c.DiaDoMes, c.DataInicio, c.DataFim, c.AlteracoesAPartirDe), ExecucaoMissaoResponse.DoResultado(r.Execucao));
}

public sealed record ConfiguracaoRecorrenciaResponse(TipoRecorrencia Tipo, IReadOnlyList<DayOfWeek> DiasSemana,
    int? DiaDoMes, DateOnly DataInicio, DateOnly? DataFim, DateOnly? AlteracoesAPartirDe);

public sealed record ExecucaoMissaoResponse(long? OcorrenciaId, bool PodeConcluir, bool Concluida, string? CodigoBloqueio,
    DateOnly Hoje, string FusoHorario, DateOnly? DataPrevista, DateOnly? ProximaData,
    DateTimeOffset? ConcluidaEm, bool Atrasada, int? Pontuacao)
{
    public static ExecucaoMissaoResponse DoResultado(ExecucaoMissaoResult r) => new(r.OcorrenciaId,
        r.PodeConcluir, r.Concluida, r.CodigoBloqueio, r.Hoje, r.FusoHorario, r.DataPrevista,
        r.ProximaData, r.ConcluidaEm, r.Atrasada, r.Pontuacao);
}
