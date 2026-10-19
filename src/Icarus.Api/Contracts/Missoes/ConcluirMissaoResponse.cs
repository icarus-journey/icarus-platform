namespace Icarus.Api.Contracts.Missoes;

public sealed record ConcluirMissaoResponse(
    long Id,
    DateTimeOffset ConcluidaEm,
    bool ConcluidaComAtraso,
    int PontuacaoCreditada,
    long SaldoPontosAtual);
