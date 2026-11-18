using BibliotecaApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BibliotecaApi.Controllers.V1
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class RootController: ControllerBase
    {
        private readonly IAuthorizationService authorizationService;

        public RootController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet(Name = "ObtenerRootV1")]
        [AllowAnonymous]
        public async Task<IEnumerable<DatosHATEOASDTO>> Get()
        {
            var datosHATEOAS = new List<DatosHATEOASDTO>();

            var esAdmin = await authorizationService.AuthorizeAsync(User, "esadmin");

            //Acciones que cualquiera puede ejecutar

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("ObtenerRootV1", new { })!,
                descripcion: "self",
                metodo: "GET"));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("ObtenerAutoresV1", new { })!,
                descripcion: "autores-obtener",
                metodo: "GET"));


            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("RegistrarUsuarioV1", new { })!,
                descripcion: "usuarios-registrar",
                metodo: "POST"));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("LoginUsuarioV1", new { })!,
                descripcion: "usuarios-login",
                metodo: "POST"));



            if (User.Identity!.IsAuthenticated)
            {
                // Acciones que solo puede ejecutar un usuario autenticado
                
                datosHATEOAS.Add(new DatosHATEOASDTO(
                    enlace: Url.Link("ActualizarUsuarioV1", new { })!,
                    descripcion: "usuarios-actualizar",
                    metodo: "PUT"));

                datosHATEOAS.Add(new DatosHATEOASDTO(
                    enlace: Url.Link("RenovarTokenUsuarioV1", new { })!,
                    descripcion: "token-renovar",
                    metodo: "GET"));
            }

            if (esAdmin.Succeeded)
            {
            // Acciones que solo puede ejecutar un administrador
            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("CrearAutorV1", new { })!,
                descripcion: "autores-crear",
                metodo: "POST"));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("CrearAutoresV1", new { })!,
                descripcion: "autores-crear",
                metodo: "POST"));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("CrearLibroV1", new { })!,
                descripcion: "libros-crear",
                metodo: "POST"));

            datosHATEOAS.Add(new DatosHATEOASDTO(
                enlace: Url.Link("ObtenerUsuariosV1", new { })!,
                descripcion: "usuarios-obtener",
                metodo: "GET"));
                
            }

            return datosHATEOAS;
        }
    }
}
