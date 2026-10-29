namespace Icarus.Domain.Entities;

public sealed class MedicaoCampanha
{
    public long Id { get; set; }
    public long CampanhaId { get; init; }
    public decimal? ValorAnterior { get; init; }
    public decimal ValorAtual { get; init; }
    public decimal ValorMensurado { get; init; }
    public string Unidade { get; init; } = string.Empty;
    public DateTimeOffset RegistradaEm { get; init; }
    public string? FusoHorario { get; init; }
    public Guid OperacaoId { get; init; }
    public Campanha Campanha { get; init; } = null!;
}
