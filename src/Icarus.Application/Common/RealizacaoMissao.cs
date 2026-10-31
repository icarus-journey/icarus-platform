using Icarus.Application.Exceptions;
using FusoDoUsuario = Icarus.Application.Common.FusoHorario;

namespace Icarus.Application.Common;

public sealed record RealizacaoMissao(DateTimeOffset Instante, string FusoHorario)
{
    // PostgreSQL preserva microssegundos; primeira resposta e retry precisam ser idênticos.
    public static DateTimeOffset NaPrecisaoPersistida(DateTimeOffset instante)
        => new(instante.UtcDateTime.Ticks / 10 * 10, TimeSpan.Zero);

    public static RealizacaoMissao Resolver(DateOnly? data, TimeOnly? horario, DateTimeOffset agora, string? fuso)
    {
        fuso = FusoDoUsuario.EhValido(fuso) ? fuso! : "UTC";
        if (data.HasValue != horario.HasValue)
            throw new EdicaoMissaoException("realizacao_incompleta", "Informe a data e o horário em que você fez a missão.");
        if (data is null) return new(NaPrecisaoPersistida(agora), fuso);

        var zona = TimeZoneInfo.FindSystemTimeZoneById(fuso);
        var local = data.Value.ToDateTime(horario!.Value, DateTimeKind.Unspecified);
        if (zona.IsInvalidTime(local))
            throw new EdicaoMissaoException("horario_inexistente", "Esse horário não existe nessa data. Escolha outro horário.");
        if (zona.IsAmbiguousTime(local))
            throw new EdicaoMissaoException("horario_ambiguo", "Esse horário se repete nessa data. Escolha outro horário.");
        var instante = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zona));
        if (instante > agora)
            throw new EdicaoMissaoException("realizacao_futura", "Escolha um momento que já passou.");
        return new(NaPrecisaoPersistida(instante), fuso);
    }
}
