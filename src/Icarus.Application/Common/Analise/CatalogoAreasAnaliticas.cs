using Icarus.Domain.Enums;

namespace Icarus.Application.Common.Analise;

public sealed record AreaAnalitica(AreaDaVida Area, string Codigo, string Titulo);
public static class CatalogoAreasAnaliticas
{
    public static readonly IReadOnlyList<AreaAnalitica> Areas = [
        new(AreaDaVida.CorpoESaude, "CORPO_E_SAUDE", "Corpo e saúde"),
        new(AreaDaVida.SonoERecuperacao, "SONO_E_RECUPERACAO", "Sono e recuperação"),
        new(AreaDaVida.MenteEEmocoes, "MENTE_E_EMOCOES", "Mente e emoções"),
        new(AreaDaVida.EstudosEAprendizado, "ESTUDOS_E_APRENDIZADO", "Estudos e aprendizado"),
        new(AreaDaVida.TrabalhoECarreira, "TRABALHO_E_CARREIRA", "Trabalho e carreira"),
        new(AreaDaVida.Financas, "FINANCAS", "Finanças"),
        new(AreaDaVida.RelacionamentosEComunidade, "RELACIONAMENTOS_E_COMUNIDADE", "Relacionamentos e comunidade"),
        new(AreaDaVida.CasaEVidaPratica, "CASA_E_VIDA_PRATICA", "Casa e vida prática"),
        new(AreaDaVida.LazerECriatividade, "LAZER_E_CRIATIVIDADE", "Lazer e criatividade"),
        new(AreaDaVida.PropositoEEspiritualidade, "PROPOSITO_E_ESPIRITUALIDADE", "Propósito e espiritualidade")
    ];

    public static bool EhMapeada(string codigo) => codigo is "CORPO_E_SAUDE" or "SONO_E_RECUPERACAO"
        or "ESTUDOS_E_APRENDIZADO" or "TRABALHO_E_CARREIRA" or "CASA_E_VIDA_PRATICA" or "LAZER_E_CRIATIVIDADE";
}
