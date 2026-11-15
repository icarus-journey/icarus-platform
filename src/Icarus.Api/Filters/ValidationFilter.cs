using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Icarus.Api.Filters;

/// <summary>
/// Valida cada argumento da action que tenha um <see cref="IValidator{T}"/> registrado e,
/// se algum for inválido, responde 400 com <see cref="ValidationProblemDetails"/> sem executar a action.
/// </summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var modelState = new ModelStateDictionary();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());

            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            foreach (var failure in result.Errors)
            {
                modelState.AddModelError(ToCamelCase(failure.PropertyName), failure.ErrorMessage);
            }
        }

        if (!modelState.IsValid)
        {
            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(context.HttpContext, modelState);

            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        await next();
    }

    // As chaves dos erros acompanham os nomes do JSON (camelCase) para o app mapear cada erro ao campo do formulário.
    private static string ToCamelCase(string propertyPath)
        => string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
