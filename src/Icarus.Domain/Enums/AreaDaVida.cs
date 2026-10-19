namespace Icarus.Domain.Enums;

/// <summary>
/// Área da vida de um Épico ou Campanha — catálogo fixo do RF-03.1 (icarus-context,
/// docs/product-specification/04-requisitos.md). Missão não possui área própria — herda a
/// do seu Épico (via Campanha) quando houver. RF-03.2: a área do épico é imutável (não existe
/// nenhum caso de uso de edição de épico até agora, então a regra já é respeitada por omissão).
/// </summary>
public enum AreaDaVida
{
    CorpoESaude,
    SonoERecuperacao,
    MenteEEmocoes,
    EstudosEAprendizado,
    TrabalhoECarreira,
    Financas,
    RelacionamentosEComunidade,
    CasaEVidaPratica,
    LazerECriatividade,
    PropositoEEspiritualidade
}
