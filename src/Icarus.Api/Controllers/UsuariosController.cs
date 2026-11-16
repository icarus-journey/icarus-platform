using System.Security.Claims;
using Icarus.Api.Contracts.Usuarios;
using Icarus.Application.Common;
using Icarus.Application.UseCases.AtualizarFotoPerfil;
using Icarus.Application.UseCases.IniciarOnboarding;
using Icarus.Application.UseCases.ObterFotoPerfil;
using Icarus.Application.UseCases.ObterPerfil;
using Icarus.Application.UseCases.ObterStatusOnboarding;
using Icarus.Application.UseCases.AtualizarPerfil;
using Icarus.Application.UseCases.AtualizarFuso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.Controllers;

/// <summary>Controller fino: traduz HTTP para o caso de uso e de volta. Sem regra de negócio aqui.</summary>
[ApiController]
[Route("api/[controller]")]
public class UsuariosController(
    ObterPerfilUseCase obterPerfil,
    AtualizarFotoPerfilUseCase atualizarFotoPerfil,
    ObterFotoPerfilUseCase obterFotoPerfil,
    IniciarOnboardingUseCase iniciarOnboarding,
    ObterStatusOnboardingUseCase obterStatusOnboarding,
    AtualizarPerfilUseCase atualizarPerfil,
    AtualizarFusoUseCase atualizarFuso) : ControllerBase
{
    /// <summary>
    /// Perfil do usuário (RF-01). Exige <c>[Authorize]</c> e o id da rota tem que ser o
    /// mesmo do token (RF-02.4) — id de outro usuário devolve <c>404</c>, igual a um id
    /// que não existe, pra não confirmar a existência de uma conta que não é a sua.
    /// </summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPerfil(Guid id, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ObterPerfilCommand(usuarioIdAutenticado, id);

        var result = await obterPerfil.ExecuteAsync(command, cancellationToken);

        return Ok(new UsuarioResponse(result.Id, result.Nome, result.Email, result.DataNascimento,
            result.Estuda, result.CargaHorariaEstudoSemanal, result.Trabalha,
            result.CargaHorariaTrabalhoSemanal, result.FusoHorario, result.TemFoto));
    }

    [Authorize]
    [HttpPut("{id:guid}/perfil")]
    [ProducesResponseType<UsuarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarPerfil(Guid id, AtualizarPerfilRequest request, CancellationToken cancellationToken)
    {
        var autenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new AtualizarPerfilCommand(autenticado, id, request.Estuda,
            request.CargaHorariaEstudoSemanal, request.Trabalha, request.CargaHorariaTrabalhoSemanal);
        var result = await atualizarPerfil.ExecuteAsync(command, cancellationToken);
        return Ok(new UsuarioResponse(result.Id, result.Nome, result.Email, result.DataNascimento,
            result.Estuda, result.CargaHorariaEstudoSemanal, result.Trabalha,
            result.CargaHorariaTrabalhoSemanal, result.FusoHorario, result.TemFoto));
    }

    [Authorize]
    [HttpPut("{id:guid}/fuso")]
    [ProducesResponseType<AtualizarFusoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarFuso(Guid id, AtualizarFusoRequest request, CancellationToken cancellationToken)
    {
        var autenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await atualizarFuso.ExecuteAsync(new AtualizarFusoCommand(autenticado, id, request.FusoHorario), cancellationToken);
        return Ok(new AtualizarFusoResponse(result.Id, result.FusoHorario));
    }

    /// <summary>
    /// Troca a foto de perfil do usuário (JPEG ou PNG, até 5 MB). Sempre sobrescreve a
    /// anterior, se houver.
    /// </summary>
    [Authorize]
    [HttpPut("{id:guid}/foto")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_500_000)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarFoto(Guid id, IFormFile arquivo, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        await using var conteudo = arquivo.OpenReadStream();
        var command = new AtualizarFotoPerfilCommand(usuarioIdAutenticado, id, conteudo, arquivo.ContentType, arquivo.Length);
        await atualizarFotoPerfil.ExecuteAsync(command, cancellationToken);

        return NoContent();
    }

    /// <summary>Busca a foto de perfil do usuário. <c>404</c> se ele ainda não tiver uma.</summary>
    [Authorize]
    [HttpGet("{id:guid}/foto")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterFoto(Guid id, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ObterFotoPerfilCommand(usuarioIdAutenticado, id);

        var result = await obterFotoPerfil.ExecuteAsync(command, cancellationToken);

        return File(result.Conteudo, result.ContentType);
    }

    /// <summary>
    /// Inicia a geração de épico+campanha+missões por IA a partir das respostas do
    /// onboarding (RF-01.5/RF-13). Assíncrono: devolve 202 com o id do trabalho; o resultado
    /// é consultado em <see cref="ObterStatusOnboarding"/>.
    /// </summary>
    [Authorize]
    [HttpPost("{id:guid}/onboarding")]
    [ProducesResponseType<IniciarOnboardingResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IniciarOnboarding(Guid id, IniciarOnboardingRequest request, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var respostas = request.Respostas
            .Select(r => new RespostaOnboarding(r.Pergunta, r.Respostas))
            .ToList();

        var command = new IniciarOnboardingCommand(usuarioIdAutenticado, id, respostas);
        var result = await iniciarOnboarding.ExecuteAsync(command, cancellationToken);

        return AcceptedAtAction(nameof(ObterStatusOnboarding), new { id, jobId = result.JobId }, new IniciarOnboardingResponse(result.JobId));
    }

    /// <summary>Status (e, quando concluído, o resultado) de um trabalho de onboarding por IA.</summary>
    [Authorize]
    [HttpGet("{id:guid}/onboarding/{jobId:guid}")]
    [ProducesResponseType<StatusOnboardingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterStatusOnboarding(Guid id, Guid jobId, CancellationToken cancellationToken)
    {
        var usuarioIdAutenticado = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ObterStatusOnboardingCommand(usuarioIdAutenticado, id, jobId);
        var result = await obterStatusOnboarding.ExecuteAsync(command, cancellationToken);

        EpicoOnboardingResponse? epico = result.Epico is null
            ? null
            : new EpicoOnboardingResponse(
                result.Epico.Id, result.Epico.Titulo, result.Epico.Descricao, result.Epico.AreaDaVida,
                result.Epico.Dificuldade, result.Epico.Pontuacao, result.Epico.PrazoInicio, result.Epico.PrazoFim, result.Epico.Estado,
                new CampanhaOnboardingResponse(
                    result.Epico.Campanha.Id, result.Epico.Campanha.Titulo, result.Epico.Campanha.Descricao, result.Epico.Campanha.AreaDaVida,
                    result.Epico.Campanha.Dificuldade, result.Epico.Campanha.Pontuacao, result.Epico.Campanha.PrazoInicio, result.Epico.Campanha.PrazoFim,
                    result.Epico.Campanha.ValorMensurado, result.Epico.Campanha.Unidade, result.Epico.Campanha.ValorAtual, result.Epico.Campanha.Estado,
                    result.Epico.Campanha.Missoes
                        .Select(m => new MissaoOnboardingResponse(m.Id, m.Titulo, m.Descricao, m.Beneficio, m.Dificuldade, m.Pontuacao, m.DataLimite, m.Horario, m.Status))
                        .ToList()));

        return Ok(new StatusOnboardingResponse(result.Status, epico, result.ErroMensagem));
    }
}
