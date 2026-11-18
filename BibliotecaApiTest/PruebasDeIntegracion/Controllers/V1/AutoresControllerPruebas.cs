using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using BibliotecaApiTest.Utilidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BibliotecaApiTest.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        private static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            //Prueba

            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres = "Juan", Apellidos = "Perez" });
            context.Autores.Add(new Autor() { Nombres = "Pedro", Apellidos = "Jimenez" });
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();

            //Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            respuesta.EnsureSuccessStatusCode();
             
            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions);

            Assert.AreEqual(expected: 1, actual: autor.Id);
        }

        [TestMethod]
        public async Task Post_Devuelve404_CuandoUsuarioNoEstaAutenticado()
        {
            // preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var cliente = factory.CreateClient();
            var autorCreacionDTO = new AutorCreacionDTO()
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioNoEsAdmin()
        {
            // preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);
            var token = await CrearUsuario(nombreBD, factory);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var autorCreacionDTO = new AutorCreacionDTO()
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve201_CuandoUsuarioEsAdmin()
        {
            // preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var claims = new List<Claim> { adminClaim };

            var token = await CrearUsuario(nombreBD, factory, claims);

            var cliente = factory.CreateClient();

            cliente.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO()
            {
                Nombres = "Miguel",
                Apellidos = "Hernandez",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            respuesta.EnsureSuccessStatusCode();

            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }
    }
}
