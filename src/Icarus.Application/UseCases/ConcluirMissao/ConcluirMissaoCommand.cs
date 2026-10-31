namespace Icarus.Application.UseCases.ConcluirMissao;

public sealed record ConcluirMissaoCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado, long MissaoId, long? OcorrenciaId = null,
    DateOnly? DataRealizacao = null, TimeOnly? HorarioRealizacao = null, DateOnly? DataPrevistaEsperada = null, string? FusoHorarioEsperado = null);
