using BibliotecaApi.Datos;
using BibliotecaApi.DTOs;
using BibliotecaApi.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().Options);
var filter = new FiltroValidacionLibro(db);
foreach (var ids in new List<int>[] { [], null })
{
    var action = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
    var context = new ActionExecutingContext(action, new List<IFilterMetadata>(),
        new Dictionary<string, object> { ["libroCreacionDTO"] = new LibroCreacionDTO { Titulo = "Libro", AutoresIds = ids } }, new object());
    var called = false;
    await filter.OnActionExecutionAsync(context, () =>
    {
        called = true;
        return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), new object()));
    });
    if (called || context.Result is not BadRequestObjectResult { Value: ValidationProblemDetails problem } ||
        !problem.Errors.ContainsKey("AutoresIds"))
        throw new Exception("FAIL: autores vacíos/null deben devolver HTTP 400 con errores de AutoresIds.");
}
Console.WriteLine("PASS: autores vacíos/null devuelven HTTP 400 y no ejecutan la acción.");
