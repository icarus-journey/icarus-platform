namespace Icarus.Application.UseCases.AtualizarFuso;

public sealed record AtualizarFusoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, string FusoHorario);
