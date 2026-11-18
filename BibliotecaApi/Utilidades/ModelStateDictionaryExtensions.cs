using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Runtime.CompilerServices;

namespace BibliotecaApi.Utilidades
{
    public static class ModelStateDictionaryExtensions
    {
        public static BadRequestObjectResult ConstruirProblemDetail(
            this ModelStateDictionary modelState)
        {
            var problemDetails = new ValidationProblemDetails(modelState)
            {
                Title = "Se encontraron errores de validación",
                Status = StatusCodes.Status400BadRequest,
            };
            return new BadRequestObjectResult(problemDetails);
        }
    }
}
