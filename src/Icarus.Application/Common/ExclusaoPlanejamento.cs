using Icarus.Domain.Entities;
using Icarus.Domain.Enums;

namespace Icarus.Application.Common;

public static class ExclusaoPlanejamento
{
    public static bool ExcluirMissao(Missao missao, DateOnly hoje)
    {
        if (missao.Status == StatusMissao.Excluida) return true;
        var recorrente = missao.Recorrencia is not null;
        if (recorrente ? missao.Status is not (StatusMissao.Ativa or StatusMissao.Inativa)
            : missao.Status != StatusMissao.Ativa || missao.ConcluidaEm is not null
                || missao.Ocorrencias.Any(o => o.Status is StatusOcorrencia.Concluida or StatusOcorrencia.ConcluidaComAtraso))
            return false;

        missao.Status = StatusMissao.Excluida;
        foreach (var ocorrencia in missao.Ocorrencias)
            if (ocorrencia.Status is StatusOcorrencia.Pendente or StatusOcorrencia.Atrasada
                && (!recorrente || ocorrencia.DataPrevista >= hoje))
                ocorrencia.Status = StatusOcorrencia.Excluida;
        return true;
    }

    public static void ExcluirCampanha(Campanha campanha, DateOnly hoje)
    {
        foreach (var missao in campanha.Missoes) ExcluirMissao(missao, hoje);
        campanha.Estado = EstadoCampanha.Excluida;
    }
}
