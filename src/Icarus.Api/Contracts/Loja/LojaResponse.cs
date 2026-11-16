using Icarus.Domain.Enums;

namespace Icarus.Api.Contracts.Loja;

public sealed record LojaResponse(long SaldoPontos, IReadOnlyList<RecompensaResponse> Recompensas,
    ProtecaoResponse Protecao, IReadOnlyList<ResgateResponse> Resgates);
public sealed record RecompensaResponse(long Id, string Titulo, string? Descricao, int Custo,
    EstadoRecompensa Estado, bool Imutavel, long? ResgateId);
public sealed record ProtecaoResponse(string Titulo, int Custo, int Quantidade, int Limite);
public sealed record ResgateResponse(long Id, long RecompensaId, string Titulo, string? Descricao,
    int Custo, bool Protecao, EstadoResgate Estado, DateTimeOffset ResgatadoEm,
    DateTimeOffset? EstornadoEm, DateTimeOffset? UtilizadoEm);
