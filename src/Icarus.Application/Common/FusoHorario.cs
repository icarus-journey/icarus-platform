namespace Icarus.Application.Common;

public static class FusoHorario
{
    /// <summary>Registros legados sem fuso válido usam UTC até a sincronização do perfil.</summary>
    public static DateOnly DataLocal(DateTimeOffset agora, string? identificador)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora,
            EhValido(identificador) ? TimeZoneInfo.FindSystemTimeZoneById(identificador!) : TimeZoneInfo.Utc).DateTime);

    public static bool EhValido(string? identificador)
        => identificador is { Length: > 0 and <= 100 }
            && (identificador == "UTC" ||
                (TimeZoneInfo.TryFindSystemTimeZoneById(identificador, out var fuso) && fuso.HasIanaId));
}
