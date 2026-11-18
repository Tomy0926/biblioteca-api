using BibliotecaApi.Entidades;
using BibliotecaApiTest.Utilidades;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;

namespace BibliotecaApiTest.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class ComentariosControllerPruebas: BasePruebas
    {
        private readonly string url = "/api/v1/libros/1/comentarios";
        string nombreBD = Guid.NewGuid().ToString();

        private async Task CrearDataPrueba()
        {
            var context = ConstruirContext(nombreBD);
            var autor = new Autor { Nombres = "Autor 1", Apellidos = "Apellido 1" };
            context.Add(autor);
            await context.SaveChangesAsync();

            var libro = new Libro { Titulo = "Libro 1" };
            libro.Autores.Add(new AutorLibro { Autor = autor });
            context.Add(libro);
            await context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task Delete_Devuelve204_CuandoUsuarioBorraSuPropioComentario()
        {
            // preparación
            await CrearDataPrueba();

            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var token = await CrearUsuario(nombreBD, factory);

            var context = ConstruirContext(nombreBD);

            var usuario = await context.Users.FirstAsync();

            var comentario = new Comentario
            {
                Cuerpo = "Comentario de prueba",
                UsuarioId = usuario!.Id,
                libroId = 1
            };

            context.Add(comentario);
            await context.SaveChangesAsync();

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            // Prueba
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");

            // Verificación

            Assert.AreEqual(expected: HttpStatusCode.NoContent, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Devuelve403_CuandoUsuarioIntentaBorrarComentarioDeOtroUsuario()
        {
            // preparación
            await CrearDataPrueba();

            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var emailCreadorComentario = "creador-comentario@hotmail.com";

            var context = ConstruirContext(nombreBD);
            var usuarioCreadorComentario = await context.Users.FirstAsync();

            var comentario = new Comentario
            {
                Cuerpo = "Comentario de prueba",
                UsuarioId = usuarioCreadorComentario!.Id,
                libroId = 1
            };

            context.Add(comentario);
            await context.SaveChangesAsync();

            var tokenUsuarioDistinto = await CrearUsuario(nombreBD, factory, [], "usuarioDistinto@hotmail.com");

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", tokenUsuarioDistinto);
            // Prueba
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");

            // Verificación

            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }
    }
}
