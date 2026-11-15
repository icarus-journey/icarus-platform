namespace Icarus.Application.UseCases.Logout;

/// <summary>Entrada do caso de uso de logout (RF-02.2). Contrato interno, desacoplado do HTTP.</summary>
public sealed record LogoutCommand(string RefreshToken);
