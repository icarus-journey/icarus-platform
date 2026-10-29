namespace Icarus.Application.UseCases.DesfazerResgate;

public sealed record DesfazerResgateCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long ResgateId);
