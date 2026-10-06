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
        if (exception is not EmailJaCadastradoException)
        {
            // Excecao inesperada: nivel Error. As excecoes de negocio conhecidas (ex.:
            // EmailJaCadastradoException) ja sao logadas por quem as lanca (o caso de uso),
            // no nivel apropriado — logar de novo aqui so duplicaria a linha.
            LogErroNaoTratado(httpContext.Request.Method, httpContext.Request.Path, exception);
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "E-mail já cadastrado",
                Detail = "Já existe uma conta associada a este e-mail."
            }
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro não tratado ao processar {Metodo} {Rota}")]
    private partial void LogErroNaoTratado(string metodo, string rota, Exception exception);
}
