using Icarus.Application.Common;
using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Missoes;

public sealed record EdicaoMissaoResponse(string VersaoContrato, long Id, long? CampanhaId, string Titulo,
    string? Descricao, string? Beneficio, Dificuldade Dificuldade, int Pontuacao, DateOnly? DataLimite,
    DateTimeOffset Horario, StatusMissao Status, DateTimeOffset? ConcluidaEm, bool Recorrente,
    string Versao, bool PodeEditar, ConfiguracaoRecorrenciaResponse? ConfiguracaoRecorrencia)
{
    public static EdicaoMissaoResponse DoResultado(EdicaoMissaoResult r) => new(r.VersaoContrato, r.Id,
        r.CampanhaId, r.Titulo, r.Descricao, r.Beneficio, r.Dificuldade, r.Pontuacao, r.DataLimite,
        r.Horario, r.Status, r.ConcluidaEm, r.Recorrente, r.Versao, r.PodeEditar,
        r.ConfiguracaoRecorrencia is not { } c ? null : new(c.Tipo, c.DiasSemana, c.DiaDoMes, c.DataInicio, c.DataFim, c.AlteracoesAPartirDe));
}

public sealed record ConfiguracaoRecorrenciaResponse(TipoRecorrencia Tipo, IReadOnlyList<DayOfWeek> DiasSemana,
    int? DiaDoMes, DateOnly DataInicio, DateOnly? DataFim, DateOnly? AlteracoesAPartirDe);
