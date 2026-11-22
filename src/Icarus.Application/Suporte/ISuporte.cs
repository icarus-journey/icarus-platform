namespace Icarus.Application.Suporte;

public interface ISuporte
{
    Task<ResumoRelato> EnviarAsync(Guid usuarioId, string chaveEnvio, string assunto, string descricao, CancellationToken cancelamento);
    Task<IReadOnlyList<ResumoRelato>> ListarAsync(Guid usuarioId, CancellationToken cancelamento);
    Task<DetalheRelato?> ConsultarAsync(Guid usuarioId, long id, CancellationToken cancelamento);
}

public enum StatusRelato { Recebido, EmAnalise, Resolvido, Indisponivel }

public sealed record ResumoRelato(long Id, string Protocolo, string Assunto, StatusRelato Status);
public sealed record DetalheRelato(long Id, string Protocolo, string Assunto, StatusRelato Status,
    string Descricao, IReadOnlyList<EtapaRelato> Historico);
public sealed record EtapaRelato(StatusRelato Status, DateTimeOffset Em);

public sealed class SuporteIndisponivelException() : Exception("Não foi possível consultar ou enviar o relato agora.");
public sealed class ChaveEnvioEmUsoException() : Exception("Esta tentativa de envio já está vinculada a outro conteúdo.");
