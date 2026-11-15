namespace Icarus.Application.UseCases.Login;

/// <summary>
/// Entrada do caso de uso de login (RF-02.1). Contrato interno, desacoplado do HTTP.
/// <paramref name="Origem"/> é o IP do cliente (RF-02.3: limite de tentativas por
/// combinação de conta normalizada + origem) — extraído na borda HTTP, já que
/// Icarus.Application não pode depender de `HttpContext`.
/// </summary>
public sealed record LoginCommand(string Email, string Senha, string Origem);
