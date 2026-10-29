namespace Icarus.Application.UseCases.ConcluirMissao;

public sealed record ConcluirMissaoResult(
    long Id,
    DateTimeOffset ConcluidaEm,
    bool ConcluidaComAtraso,
    int PontuacaoCreditada,
    long SaldoPontosAtual);
