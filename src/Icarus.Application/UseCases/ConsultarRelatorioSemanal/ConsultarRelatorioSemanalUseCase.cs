using Icarus.Application.Common.Analise;
using Icarus.Application.Common.Relatorios;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarRelatorioSemanal;

public sealed class ConsultarRelatorioSemanalUseCase(IUsuarioRepository usuarios, IRelatorioSemanalRepository repositorio, TimeProvider relogio)
{
    public async Task<ConsultarRelatorioSemanalResult> ExecuteAsync(ConsultarRelatorioSemanalCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var usuario = await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, ct) ?? throw new PerfilNaoEncontradoException();
        if (!Icarus.Application.Common.FusoHorario.EhValido(usuario.FusoHorario))
            return new("fuso_nao_informado", null, "Abra novamente o aplicativo para sincronizar o fuso e consultar sua semana.");
        var agora = relogio.GetUtcNow();
        var periodo = PeriodoAnalitico.Encerrado("semanaAnterior", usuario.FusoHorario!, agora);
        var relatorio = await repositorio.BuscarRelatorioAsync(usuario.Id, periodo.Inicio, periodo.Fim, ct);
        var trabalho = await repositorio.BuscarTrabalhoAsync(usuario.Id, periodo.Inicio, periodo.Fim, ct);
        return ApresentacaoRelatorioSemanal.Criar(periodo, relatorio, trabalho, agora);
    }
}
