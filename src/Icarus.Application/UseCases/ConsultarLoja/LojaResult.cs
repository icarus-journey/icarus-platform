using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.ConsultarLoja;

public sealed record DadosRecompensa(string Titulo, string? Descricao, int Custo);
public sealed record RecompensaResult(long Id, string Titulo, string? Descricao, int Custo,
    EstadoRecompensa Estado, bool Imutavel, long? ResgateId);
public sealed record ResgateResult(long Id, long RecompensaId, string Titulo, string? Descricao,
    int Custo, bool Protecao, EstadoResgate Estado, DateTimeOffset ResgatadoEm,
    DateTimeOffset? EstornadoEm, DateTimeOffset? UtilizadoEm);
public sealed record ProtecaoResult(string Titulo, int Custo, int Quantidade, int Limite);
public sealed record LojaResult(long SaldoPontos, IReadOnlyList<RecompensaResult> Recompensas,
    ProtecaoResult Protecao, IReadOnlyList<ResgateResult> Resgates);

public sealed class LojaOptions
{
    public int CustoProtecao { get; set; } = 500;
}
