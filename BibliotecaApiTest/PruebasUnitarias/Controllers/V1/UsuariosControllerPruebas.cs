using BibliotecaApi.Controllers.V1;
using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using BibliotecaApi.Servicios;
using BibliotecaApiTest.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaApiTest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class UsuariosControllerPruebas: BasePruebas
    {
        private string nombreDB = Guid.NewGuid().ToString();
        private UserManager<Usuario> userManager = null!;
        private SignInManager<Usuario> signInManager = null!;
        private UsuariosController controllers = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreDB);
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            var miConfiguracion = new Dictionary<string, string>
            {
                {
                    "llavejwt", "dsagdsfgDFGJURTEHFSDGFDdgfdshdfghsd"
                }
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(miConfiguracion!)
                .Build();

            var contextAccesor = Substitute.For<IHttpContextAccessor>();
            var userClaimsFactory = Substitute.For<IUserClaimsPrincipalFactory<Usuario>>();

            signInManager = Substitute.For<SignInManager<Usuario>>(userManager,
                contextAccesor, userClaimsFactory, null, null, null, null);

            var servicioUsuarios = Substitute.For<IServiciosUsuarios>();
            var mapper = ConfigurarAutoMapper();

            controllers = new UsuariosController(userManager, configuration, signInManager,
                servicioUsuarios, context, mapper);
        }

        [TestMethod]
        public async Task Registrar_DevuelveValidationProblem_CuandoNoEsExitoso()
        {
            // Preparacion

            var mensajeDeError = "prueba";
            var credenciales = new CredencialesUsuarioDTO 
            { 
                Email = "prueba@gmail.com",
                Password = "Prueba123!" 
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                .Returns(IdentityResult.Failed(new IdentityError
                {
                    Code = "Prueba",
                    Description = mensajeDeError
                }));

            // Prueba

            var respuesta = await controllers.Registrar(credenciales);  

            // Verificacion
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Registrar_DevuelveToken_CuandoEsExitoso()
        {
            // Preparacion
            var credenciales = new CredencialesUsuarioDTO 
            { 
                Email = "prueba@gmail.com",
                Password = "aA12345678!" 
            };

            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>())
                .Returns(IdentityResult.Success);

            // Prueba

            var respuesta = await controllers.Registrar(credenciales);  

            // Verificacion
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoUsuarioNoExiste()
        {
            //preparacion

            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@gmail.com",
                Password = "aA12345678!"
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario?>(null!));

            // Prueba
            var respuesta = await controllers.Login(credenciales);

            // verficiacion

            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoLoginEsIncorrecto()
        {
            //preparacion

            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@gmail.com",
                Password = "aA12345678!"
            };

            var usuario = new Usuario
            {
                Email = credenciales.Email,
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario?>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false)
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Failed));

            // Prueba

            var respuesta = await controllers.Login(credenciales);

            // verficiacion

            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto",
                actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveToken_CuandoLoginEsExitoso()
        {
            //preparacion

            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@gmail.com",
                Password = "aA12345678!"
            };

            var usuario = new Usuario
            {
                Email = credenciales.Email,
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario?>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false)
                .Returns(Task.FromResult(Microsoft.AspNetCore.Identity.SignInResult.Success));

            // Prueba

            var respuesta = await controllers.Login(credenciales);

            // verficiacion
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }

    }
}
