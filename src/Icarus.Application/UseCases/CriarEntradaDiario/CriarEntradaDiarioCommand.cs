using Icarus.Domain.Enums;

namespace Icarus.Application.UseCases.CriarEntradaDiario;

public sealed record CriarEntradaDiarioCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, TipoEntradaDiario Tipo, DateOnly DataRegistro, string Texto);
