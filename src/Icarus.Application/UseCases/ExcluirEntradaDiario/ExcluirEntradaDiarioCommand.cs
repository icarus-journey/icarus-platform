namespace Icarus.Application.UseCases.ExcluirEntradaDiario;

public sealed record ExcluirEntradaDiarioCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long EntradaId);
