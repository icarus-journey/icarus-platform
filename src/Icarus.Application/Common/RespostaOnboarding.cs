namespace Icarus.Application.Common;

/// <summary>
/// Par pergunta+resposta do questionário de onboarding, em texto legível — o mobile já exibe
/// os labels ao usuário, então manda o texto pronto; o backend só repassa pro worker de IA,
/// que é agnóstico ao catálogo de perguntas (nenhum enum é duplicado nos dois lados).
/// </summary>
public sealed record RespostaOnboarding(string Pergunta, IReadOnlyList<string> Respostas);
