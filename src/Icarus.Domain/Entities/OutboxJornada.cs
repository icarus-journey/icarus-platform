namespace Icarus.Domain.Entities;

public sealed class OutboxJornada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public DateTimeOffset CriadaEm { get; set; }
    public DateTimeOffset? ProximaTentativaEm { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
}
