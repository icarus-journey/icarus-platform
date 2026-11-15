namespace Icarus.Domain.Entities;

/// <summary>Versão observada do planejamento, sem texto livre e sem reescrever versões anteriores.</summary>
public sealed class RegistroPlanejamentoOcorrencia
{
    public long Id { get; set; }
    public long OcorrenciaId { get; set; }
    public DateOnly DataPrevista { get; set; }
    public bool Ativa { get; set; }
    public DateTimeOffset RegistradoEm { get; set; }
    public Ocorrencia Ocorrencia { get; set; } = null!;
}
