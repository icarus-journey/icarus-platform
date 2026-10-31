using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public static class RegrasRecorrencia
{
    public static bool Concluida(Ocorrencia o) => o.ConcluidaEm is not null
        || o.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso;

    public static bool Pendente(Ocorrencia o) => !Concluida(o)
        && o.Status is StatusOcorrencia.Pendente or StatusOcorrencia.Atrasada;

    public static DateOnly Inicio(Missao m) => m.Campanha is { } c
        ? (m.Recorrencia is { } r && r.DataInicio > c.PrazoInicio ? r.DataInicio : c.PrazoInicio)
        : m.Recorrencia?.DataInicio ?? DateOnly.MinValue;

    public static DateOnly Fim(Missao m)
    {
        var fim = m.Campanha?.PrazoFim ?? DateOnly.MaxValue;
        return m.Recorrencia?.DataFim is { } r && r < fim ? r : fim;
    }

    public static bool DentroDosLimites(Missao m, DateOnly data) => data >= Inicio(m) && data <= Fim(m);

    public static (DateOnly Inicio, DateOnly Fim) Janela(Missao m, DateOnly hoje, bool somenteFuturo = false)
    {
        var inicio = somenteFuturo ? hoje.AddDays(1) : hoje;
        if (Inicio(m) > inicio) inicio = Inicio(m);
        if (m.Recorrencia?.AlteracoesAPartirDe is { } vigencia && vigencia > inicio) inicio = vigencia;
        var fim = hoje.AddDays(GeradorDeDatasRecorrencia.DiasDaJanelaPadrao);
        if (Fim(m) < fim) fim = Fim(m);
        return (inicio, fim);
    }

    /// <summary>Preserva vencidas/concluídas; não reabre nenhuma linha excluída.</summary>
    public static bool EncerrarSeNecessario(Missao m, DateOnly hoje)
    {
        if (m.Recorrencia is null || m.Status != StatusMissao.Ativa) return false;
        if (hoje <= Fim(m) && m.Campanha?.Estado is not (EstadoCampanha.Concluida or EstadoCampanha.Excluida)) return false;
        m.Status = StatusMissao.Inativa;
        foreach (var o in m.Ocorrencias.Where(o => Pendente(o) && o.DataPrevista >= hoje)) o.Status = StatusOcorrencia.Excluida;
        return true;
    }

    /// <summary>Hoje tem prioridade, seguido pelo atraso mais antigo. Nunca seleciona futuro.</summary>
    public static Ocorrencia? Selecionar(Missao m, DateOnly hoje)
    {
        var elegiveis = m.Ocorrencias.Where(o => o.DataPrevista <= hoje && DentroDosLimites(m, o.DataPrevista)
            && (Pendente(o) || Concluida(o))).ToList();
        var doDia = elegiveis.SingleOrDefault(o => o.DataPrevista == hoje);
        if (doDia is not null) return doDia;
        var atrasada = elegiveis.Where(Pendente).OrderBy(o => o.DataPrevista).FirstOrDefault();
        if (atrasada is not null) return atrasada;
        if (m.Recorrencia is null)
        {
            var concluida = elegiveis.Where(Concluida).OrderByDescending(o => o.DataPrevista).FirstOrDefault();
            if (concluida is not null) return concluida;
            // Planejamento legado explícito sem ocorrência: materializa somente sua data válida.
            var prevista = m.DataLimite ?? hoje;
            if (m.Ocorrencias.Count == 0 && prevista <= hoje && DentroDosLimites(m, prevista))
                return new Ocorrencia { Missao = m, MissaoId = m.Id, DataPrevista = prevista };
        }
        else if (m.Ocorrencias.All(o => o.DataPrevista != hoje))
        {
            // Recupera apenas o dia atual previsto na regra vigente, sem reconstruir dias antigos.
            var janela = Janela(m, hoje);
            if (janela.Inicio <= hoje && janela.Fim >= hoje && GeradorDeDatasRecorrencia.Gerar(hoje, hoje,
                Enum.Parse<TipoRecorrencia>(m.Recorrencia.Tipo, true), m.Recorrencia.Regra).Count > 0)
                return new Ocorrencia { Missao = m, MissaoId = m.Id, DataPrevista = hoje, Planejamento = PlanejamentoOcorrencia.Capturar(m) };
        }
        return null;
    }
}
