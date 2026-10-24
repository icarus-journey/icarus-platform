using System.Text.Json.Serialization;

namespace Icarus.Application.Common.Analise;

public sealed record PeriodoAnalitico(DateOnly Inicio, DateOnly Fim, string FusoHorario)
{
    public static PeriodoAnalitico Encerrado(string periodo, string fusoHorario, DateTimeOffset agora)
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(fusoHorario);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora, fuso).DateTime);
        var primeiroDoMes = new DateOnly(hoje.Year, hoje.Month, 1);
        var inicioSemanaAtual = hoje.AddDays(-((7 + (int)hoje.DayOfWeek - (int)DayOfWeek.Monday) % 7));
        return periodo switch
        {
            "semanaAnterior" => new(inicioSemanaAtual.AddDays(-7), inicioSemanaAtual.AddDays(-1), fusoHorario),
            "mesAnterior" => new(primeiroDoMes.AddMonths(-1), primeiroDoMes.AddDays(-1), fusoHorario),
            "seisMeses" => new(primeiroDoMes.AddMonths(-6), primeiroDoMes.AddDays(-1), fusoHorario),
            _ => throw new ArgumentException("Período inválido.", nameof(periodo))
        };
    }

    [JsonIgnore]
    public DateTimeOffset InicioUtc => LimiteUtc(Inicio);
    [JsonIgnore]
    public DateTimeOffset FimExclusivoUtc => LimiteUtc(Fim.AddDays(1));
    public PeriodoAnalitico SemanaAnterior() => new(Inicio.AddDays(-7), Fim.AddDays(-7), FusoHorario);

    private DateTimeOffset LimiteUtc(DateOnly data)
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(FusoHorario);
        var local = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // Há zonas em que a mudança de relógio elimina a meia-noite civil.
        while (fuso.IsInvalidTime(local)) local = local.AddMinutes(1);
        var deslocamento = fuso.IsAmbiguousTime(local) ? fuso.GetAmbiguousTimeOffsets(local).Max() : fuso.GetUtcOffset(local);
        return new DateTimeOffset(local, deslocamento).ToUniversalTime();
    }
}
