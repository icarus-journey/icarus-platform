using Icarus.Application.Common.Jornada;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarJornada;

public sealed class ConsultarJornadaUseCase(IJornadaRepository repositorio, TimeProvider relogio)
{
    public async Task<ConsultarJornadaResult> ExecuteAsync(ConsultarJornadaCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var dados = await repositorio.BuscarAsync(command.UsuarioIdSolicitado, ct) ?? throw new PerfilNaoEncontradoException();
        return new(dados.SaldoPontos, CalculadorSequencia.Calcular(dados, relogio.GetUtcNow()), ProtecoesPendentes: dados.ProtecoesPendentes);
    }
}
