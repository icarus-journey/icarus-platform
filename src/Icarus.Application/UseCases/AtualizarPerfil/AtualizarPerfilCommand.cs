namespace Icarus.Application.UseCases.AtualizarPerfil;

public sealed record AtualizarPerfilCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado,
    bool? Estuda, decimal? CargaHorariaEstudoSemanal, bool? Trabalha, decimal? CargaHorariaTrabalhoSemanal);
