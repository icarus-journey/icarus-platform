using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.AtualizarEntradaDiario;

public sealed record AtualizarEntradaDiarioCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long EntradaId, TipoEntradaDiario Tipo, DateOnly DataRegistro, string Texto);
