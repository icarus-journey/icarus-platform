using System.Security.Claims;
using Icarus.Api.Contracts.Loja;
using Icarus.Application.UseCases.ConsultarLoja;
using Icarus.Application.UseCases.CriarRecompensa;
using Icarus.Application.UseCases.AtualizarRecompensa;
using Icarus.Application.UseCases.ExcluirRecompensa;
using Icarus.Application.UseCases.ResgatarRecompensa;
using Icarus.Application.UseCases.DesfazerResgate;
using Icarus.Application.UseCases.UtilizarRecompensa;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/usuarios/{usuarioId:guid}/loja")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public sealed class LojaController(
    ConsultarLojaUseCase consultarLoja,
    CriarRecompensaUseCase criar,
    AtualizarRecompensaUseCase atualizar,
    ExcluirRecompensaUseCase excluir,
    ResgatarRecompensaUseCase resgatar,
    DesfazerResgateUseCase desfazer,
    UtilizarRecompensaUseCase utilizar) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar(Guid usuarioId, CancellationToken ct)
        => Ok(ParaResponse(await consultarLoja.ExecuteAsync(new(Autenticado(), usuarioId), ct)));

    [HttpPost("recompensas")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(Guid usuarioId, RecompensaRequest dados, CancellationToken ct)
        => CreatedAtAction(nameof(Consultar), new { usuarioId },
            ParaResponse(await criar.ExecuteAsync(new(Autenticado(), usuarioId, dados.Titulo, dados.Descricao, dados.Custo), ct)));

    [HttpPatch("recompensas/{recompensaId:long}")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Atualizar(Guid usuarioId, long recompensaId, RecompensaRequest dados, CancellationToken ct)
        => Ok(ParaResponse(await atualizar.ExecuteAsync(new(Autenticado(), usuarioId, recompensaId, dados.Titulo, dados.Descricao, dados.Custo), ct)));

    [HttpDelete("recompensas/{recompensaId:long}")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Excluir(Guid usuarioId, long recompensaId, CancellationToken ct)
        => Ok(ParaResponse(await excluir.ExecuteAsync(new(Autenticado(), usuarioId, recompensaId), ct)));

    [HttpPost("recompensas/{recompensaId:long}/resgates")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resgatar(Guid usuarioId, long recompensaId, ResgateRequest dados, CancellationToken ct)
        => Ok(ParaResponse(await resgatar.ExecuteAsync(new(Autenticado(), usuarioId, recompensaId, dados.OperacaoId), ct)));

    [HttpPost("protecao/resgates")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResgatarProtecao(Guid usuarioId, ResgateRequest dados, CancellationToken ct)
        => Ok(ParaResponse(await resgatar.ExecuteAsync(new(Autenticado(), usuarioId, null, dados.OperacaoId), ct)));

    [HttpPost("resgates/{resgateId:long}/desfazer")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Desfazer(Guid usuarioId, long resgateId, CancellationToken ct)
        => Ok(ParaResponse(await desfazer.ExecuteAsync(new(Autenticado(), usuarioId, resgateId), ct)));

    [HttpPost("resgates/{resgateId:long}/utilizar")]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Utilizar(Guid usuarioId, long resgateId, CancellationToken ct)
        => Ok(ParaResponse(await utilizar.ExecuteAsync(new(Autenticado(), usuarioId, resgateId), ct)));


    private static LojaResponse ParaResponse(LojaResult resultado) => new(
        resultado.SaldoPontos,
        resultado.Recompensas.Select(r => new RecompensaResponse(r.Id, r.Titulo, r.Descricao,
            r.Custo, r.Estado, r.Imutavel, r.ResgateId)).ToList(),
        new ProtecaoResponse(resultado.Protecao.Titulo, resultado.Protecao.Custo,
            resultado.Protecao.Quantidade, resultado.Protecao.Limite),
        resultado.Resgates.Select(r => new ResgateResponse(r.Id, r.RecompensaId, r.Titulo,
            r.Descricao, r.Custo, r.Protecao, r.Estado, r.ResgatadoEm, r.EstornadoEm, r.UtilizadoEm)).ToList());

    private Guid Autenticado() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
