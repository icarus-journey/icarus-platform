namespace Icarus.Application.UseCases.RefreshToken;

/// <summary>Entrada do caso de uso de refresh (RF-02). Contrato interno, desacoplado do HTTP.</summary>
public sealed record RefreshTokenCommand(string RefreshToken);
