using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaApiTest.Utilidades
{
    public class UsuarioFalsoFiltro : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // antes de la accion
            context.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("email", "ejemplo@hotmail.com")
            }, "prueba"));

            await next();

            // Despues de la accion

        }
    }
}
