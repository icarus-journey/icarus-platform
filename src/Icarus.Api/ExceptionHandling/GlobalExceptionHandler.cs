using Icarus.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Icarus.Api.ExceptionHandling;

/// <summary>
/// Traduz exceções de negócio conhecidas em respostas ProblemDetails. Qualquer outra exceção
/// devolve <c>false</c> e cai no tratamento padrão (500 genérico, sem detalhes internos — seção 10.1 da arquitetura).
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not EmailJaCadastradoException)
        {
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
}
