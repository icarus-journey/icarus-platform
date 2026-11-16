namespace Icarus.Application.UseCases.ObterPerfil;

/// <summary>
/// Entrada do caso de uso de obter perfil (RF-01/RF-02.4). Contrato interno,
/// desacoplado do HTTP. <paramref name="UsuarioIdAutenticado"/> vem da claim do
/// token; <paramref name="UsuarioIdSolicitado"/> vem da rota — a checagem de que
/// os dois são o mesmo é a aplicação concreta do RF-02.4 ("identificador de
/// outro usuário não permite consultar o recurso").
/// </summary>
public sealed record ObterPerfilCommand(Guid UsuarioIdAutenticado, Guid UsuarioIdSolicitado);
