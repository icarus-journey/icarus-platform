using Icarus.Domain.Enums;

namespace Icarus.Infrastructure.Messaging.Contratos;

/// <summary>
/// Converte os tokens de texto do contrato com o icarus-ai (ex.: "FACIL", "DESENVOLVIMENTO_PESSOAL")
/// para os enums do domínio. Mapeamento explícito (não um conversor de enum automático) porque
/// o formato do fio é combinado entre os dois repositórios, não uma convenção genérica do .NET.
/// </summary>
public static class OnboardingWireMapeamento
{
    public static Dificuldade ParaDificuldade(string valor) => valor switch
    {
        "TRIVIAL" => Dificuldade.Trivial,
        "FACIL" => Dificuldade.Facil,
        "MEDIA" => Dificuldade.Media,
        "DIFICIL" => Dificuldade.Dificil,
        _ => throw new FormatException($"Dificuldade desconhecida recebida do icarus-ai: '{valor}'.")
    };

    public static AreaDaVida ParaAreaDaVida(string valor) => valor switch
    {
        "CORPO_E_SAUDE" => AreaDaVida.CorpoESaude,
        "SONO_E_RECUPERACAO" => AreaDaVida.SonoERecuperacao,
        "MENTE_E_EMOCOES" => AreaDaVida.MenteEEmocoes,
        "ESTUDOS_E_APRENDIZADO" => AreaDaVida.EstudosEAprendizado,
        "TRABALHO_E_CARREIRA" => AreaDaVida.TrabalhoECarreira,
        "FINANCAS" => AreaDaVida.Financas,
        "RELACIONAMENTOS_E_COMUNIDADE" => AreaDaVida.RelacionamentosEComunidade,
        "CASA_E_VIDA_PRATICA" => AreaDaVida.CasaEVidaPratica,
        "LAZER_E_CRIATIVIDADE" => AreaDaVida.LazerECriatividade,
        "PROPOSITO_E_ESPIRITUALIDADE" => AreaDaVida.PropositoEEspiritualidade,
        _ => throw new FormatException($"Área da vida desconhecida recebida do icarus-ai: '{valor}'.")
    };
}
