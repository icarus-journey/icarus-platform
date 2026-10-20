using Icarus.Application.Common.Analise;
using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;

namespace Icarus.Application.UseCases.ConsultarProgresso;

public sealed class ConsultarProgressoUseCase(IUsuarioRepository usuarios, IIndicadoresPessoaisRepository indicadores, TimeProvider relogio)
{
    public async Task<ConsultarProgressoResult> ExecuteAsync(ConsultarProgressoCommand command, CancellationToken ct)
    {
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado) throw new PerfilNaoEncontradoException();
        var usuario = await usuarios.BuscarPorIdAsync(command.UsuarioIdSolicitado, ct) ?? throw new PerfilNaoEncontradoException();
        if (!Icarus.Application.Common.FusoHorario.EhValido(usuario.FusoHorario))
            return new("fuso_nao_informado", null, null, "historico_mensurado_indisponivel", "Abra novamente o aplicativo para sincronizar o fuso deste dispositivo e consultar seu período.");
        var periodo = PeriodoAnalitico.Encerrado(command.Periodo, usuario.FusoHorario!, relogio.GetUtcNow());
        var dados = await indicadores.CalcularAsync(usuario.Id, periodo, ct);
        return new(dados.QuantidadeRegistros == 0 ? "sem_registros" : "disponivel", periodo, dados,
            "historico_mensurado_indisponivel", dados.QuantidadeRegistros == 0
                ? "Ainda não há registros neste período. Isso não significa que você não realizou atividades."
                : "Estes indicadores resumem apenas os registros do aplicativo, não sua rotina inteira.");
    }
}
