namespace Icarus.Application.UseCases.RegistrarProgressoCampanha;

public sealed record RegistrarProgressoCampanhaCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado,
    long CampanhaId, decimal ValorAtual, string VersaoEsperada, Guid OperacaoId);
