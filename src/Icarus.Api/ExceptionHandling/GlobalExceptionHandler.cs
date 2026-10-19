using Icarus.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.ExceptionHandling;

/// <summary>
/// Traduz exceções de negócio conhecidas em respostas ProblemDetails. Qualquer outra exceção
/// devolve <c>false</c> e cai no tratamento padrão (500 genérico, sem detalhes internos — seção 10.1 da arquitetura).
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            EmailJaCadastradoException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "E-mail já cadastrado",
                Detail = "Já existe uma conta associada a este e-mail."
            },
            CredenciaisInvalidasException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Title = "Credenciais inválidas",
                Detail = "E-mail ou senha inválidos."
            },
            RefreshTokenInvalidoException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                Title = "Refresh token inválido",
                Detail = "Refresh token inválido ou expirado."
            },
            LimiteDeTentativasExcedidoException => new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Title = "Muitas tentativas",
                Detail = "Muitas tentativas de login. Tente novamente mais tarde."
            },
            PerfilNaoEncontradoException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Perfil não encontrado",
                Detail = "Perfil não encontrado."
            },
            TrabalhoOnboardingNaoEncontradoException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Trabalho de onboarding não encontrado",
                Detail = "Trabalho de onboarding não encontrado."
            },
            EpicoNaoEncontradoException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Épico não encontrado",
                Detail = "Épico não encontrado."
            },
            CampanhaNaoEncontradaException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Campanha não encontrada",
                Detail = "Campanha não encontrada."
            },
            MissaoNaoEncontradaException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "Missão não encontrada",
                Detail = "Missão não encontrada."
            },
            MissaoConcluidaException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "Missão concluída",
                Detail = "Missão concluída não pode ser editada."
            },
            _ => null
        };

        if (problemDetails is null)
        {
            // Excecao inesperada: nivel Error. As excecoes de negocio conhecidas (ex.:
            // EmailJaCadastradoException, CredenciaisInvalidasException,
            // RefreshTokenInvalidoException, LimiteDeTentativasExcedidoException) ja sao
            // logadas por quem as lanca (o caso de uso), no nivel apropriado — logar de
            // novo aqui so duplicaria a linha.
            LogErroNaoTratado(httpContext.Request.Method, httpContext.Request.Path, exception);
            return false;
        }

        httpContext.Response.StatusCode = problemDetails.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro não tratado ao processar {Metodo} {Rota}")]
    private partial void LogErroNaoTratado(string metodo, string rota, Exception exception);
}
