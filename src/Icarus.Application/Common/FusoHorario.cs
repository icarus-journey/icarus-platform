namespace Icarus.Application.Common;

public static class FusoHorario
{
    public static bool EhValido(string? identificador)
        => identificador is { Length: > 0 and <= 100 }
            && (identificador == "UTC" ||
                (TimeZoneInfo.TryFindSystemTimeZoneById(identificador, out var fuso) && fuso.HasIanaId));
}
