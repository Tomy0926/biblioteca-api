using BibliotecaApi.Entidades;
using BibliotecaApi.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaApiTest.PruebasUnitarias.Servicios
{
    [TestClass]
    public class ServiciosUsuariosPruebas
    {
        private UserManager<Usuario> userManager = null!;
        private IHttpContextAccessor contexAccesor = null!;
        private ServiciosUsuarios servicioUsuarios = null!;

        [TestInitialize]
        public void Setup()
        {
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            contexAccesor = Substitute.For<IHttpContextAccessor>();
            servicioUsuarios = new ServiciosUsuarios(userManager, contexAccesor);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoNoHayClaimEmail()
        {
            // Preparacion
            var httpContext = new DefaultHttpContext();
            contexAccesor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // verificacion
            Assert.IsNull(usuario);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarUsuario_CuandoHayClaimEmail()
        {
            // Preparacion
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(usuarioEsperado));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contexAccesor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // verificacion
            Assert.IsNotNull(usuario);
            Assert.AreEqual(expected: email, actual: usuario.Email);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoUsuarioNoExiste()
        {
            // Preparacion
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<Usuario>(null!));

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims };
            contexAccesor.HttpContext.Returns(httpContext);

            //Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // verificacion
            Assert.IsNull(usuario);
        }
    }
}
