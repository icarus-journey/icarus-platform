using Icarus.Application.Exceptions;
using Icarus.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Icarus.Application.UseCases.ObterPerfil;

/// <summary>Caso de uso de obter perfil (RF-01, com a checagem de propriedade do RF-02.4).</summary>
public sealed partial class ObterPerfilUseCase(IUsuarioRepository usuarioRepository, ILogger<ObterPerfilUseCase> logger)
{
    public async Task<ObterPerfilResult> ExecuteAsync(ObterPerfilCommand command, CancellationToken cancellationToken)
    {
        // RF-02.4: identificador de outro usuário não pode consultar o recurso. Checa
        // antes de ir ao banco — nem revela, pela diferença de tempo de resposta, se o
        // id solicitado existe ou não.
        if (command.UsuarioIdAutenticado != command.UsuarioIdSolicitado)
        {
            LogAcessoNegado();
            throw new PerfilNaoEncontradoException();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(command.UsuarioIdSolicitado, cancellationToken);
        if (usuario is null)
        {
            // Não deveria acontecer (o token só existe pra um usuário que existia no
            // momento do login), mas não é impossível — ex.: conta excluída depois.
            throw new PerfilNaoEncontradoException();
        }

        return new ObterPerfilResult(usuario.Id, usuario.Nome, usuario.Email, usuario.DataNascimento,
            usuario.Estuda, usuario.CargaHorariaEstudoSemanal, usuario.Trabalha,
            usuario.CargaHorariaTrabalhoSemanal, usuario.FusoHorario, usuario.FotoChave is not null);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tentativa de acessar perfil de outro usuário rejeitada")]
    private partial void LogAcessoNegado();
}
