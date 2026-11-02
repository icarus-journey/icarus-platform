namespace Icarus.Api.Contracts.Missoes;

public sealed record ConcluirMissaoResponse(
    long Id,
    DateTimeOffset ConcluidaEm,
    bool ConcluidaComAtraso,
    int PontuacaoCreditada,
    long SaldoPontosAtual,
    bool Repetida = false,
    DateTimeOffset? RealizadaEm = null,
    string? FusoHorarioRealizacao = null,
    long? OcorrenciaId = null,
    DateOnly? DataPrevista = null,
    string VersaoContrato = "1.3.0");
