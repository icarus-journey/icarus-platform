using Icarus.Application.Common.Jornada;

namespace Icarus.Application.UseCases.ConsultarJornada;

public sealed record ConsultarJornadaResult(long SaldoPontos, SequenciaDiaria Sequencia,
    string VersaoContrato = "1.0.0");
