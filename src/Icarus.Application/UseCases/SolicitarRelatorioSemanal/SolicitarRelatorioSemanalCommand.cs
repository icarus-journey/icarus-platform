namespace Icarus.Application.UseCases.SolicitarRelatorioSemanal;
public sealed record SolicitarRelatorioSemanalCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, bool NovaTentativa = false);
