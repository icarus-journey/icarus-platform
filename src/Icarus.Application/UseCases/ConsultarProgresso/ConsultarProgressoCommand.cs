namespace Icarus.Application.UseCases.ConsultarProgresso;

public sealed record ConsultarProgressoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado,
    string Periodo = "semanaAnterior");
