using AutoMapper;
using BibliotecaApi.Controllers.V1;
using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using BibliotecaApi.Servicios;
using BibliotecaApi.Servicios.V1;
using BibliotecaApiTest.Utilidades;
using BibliotecaApiTest.Utilidades.Dobles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.CodeCoverage.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaApiTest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresControllerPruebas : BasePruebas
    {
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore outputCacheStore = null!;
        IServicioAutores servicioAutores = Substitute.For<IServicioAutores>();
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();
            servicioAutores = Substitute.For<IServicioAutores>();

            controller = new AutoresController(context, mapper, almacenadorArchivos,
                logger, outputCacheStore, servicioAutores);
        }

        [TestMethod]
        public async Task Get_Retorna404CuandoNoExiste()
        {
            // Prueba
            var respuesta = await controller.Get(1);

            // Veriificación
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorConIdExiste()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor { Nombres = "Juan", Apellidos = "Perez" });
            context.Autores.Add(new Autor { Nombres = "Pedro", Apellidos = "Jimenez" });

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Get(1);

            // Veriificación
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
        }

        [TestMethod]
        public async Task Get_DebeLlamarGetDelServicioAutores()
        {
            // Preparación

            var paginacionDTO = new PaginacionDTO(2, 3);

            // Prueba

            await controller.Get(paginacionDTO);

            // Verificacion

            await servicioAutores.Received(1).Get(paginacionDTO);

        }

        [TestMethod]
        public async Task Get_RetornarAutorConLibros_CuandoAutorTieneLibros()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);

            var libro1 = new Libro() { Titulo = "Libro 1" };
            var libro2 = new Libro() { Titulo = "Libro 2" };

            var autor = new Autor()
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Libros = new List<AutorLibro>()
                {
                    new AutorLibro() { Libro = libro1 },
                    new AutorLibro() { Libro = libro2 }
                }
            };

            context.Autores.Add(autor);

            await context.SaveChangesAsync();

            // Prueba   
            var respuesta = await controller.Get(1);

            // Veriificación
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
            Assert.AreEqual(expected: 2, actual: resultado.Libros!.Count);
        }

        [TestMethod]
        public async Task Post_DebeCrearAutor_CuandoEnviamosAutor()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);

            var nuevoAutor = new AutorCreacionDTO()
            {
                Nombres = "Juan",
                Apellidos = "Perez"
            };

            // Prueba
            var respuesta = await controller.Post(nuevoAutor);

            // Veriificación
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado);

            var context2 = ConstruirContext(nombreBD);
            var cantidad = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidad);
        }

        [TestMethod]
        public async Task Put_Retorna404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO: null!);

            // Verificación

            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorSinFoto()
        {
            //Preparación

            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "Id"
            });

            await context.SaveChangesAsync();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Juan Actualizado",
                Apellidos = "Perez Actualizado",
                Identificacion = "Id Actualizado"
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificación

            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, actual: resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Juan Actualizado", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Perez Actualizado", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id Actualizado", actual: autorActualizado.Identificacion);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!);
        }

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorConFoto()
        {
            //Preparación

            var context = ConstruirContext(nombreBD);

            var urlAnterior = "URL-1";
            var urlNueva = "URL-2";
            almacenadorArchivos.Editar(ruta: default!, contenedor: default!, archivo: default!).ReturnsForAnyArgs(urlNueva);

            context.Autores.Add(new Autor
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "Id",
                Foto = urlAnterior
            });

            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Juan Actualizado",
                Apellidos = "Perez Actualizado",
                Identificacion = "Id Actualizado",
                Foto = formFile
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificación

            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, actual: resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Juan Actualizado", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Perez Actualizado", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id Actualizado", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNueva, actual: autorActualizado.Foto);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Editar(urlAnterior, contenedor, formFile);
        }

        [TestMethod]
        public async Task Patch_Retorna400_CuandoPatchDocEsNulo()
        {
            // Prueba
            var respuesta = await controller.Patch(1, patchDocument: null!);
            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 400, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_Retorna404_CuandoAutorNoExiste()
        {
            // Preparación
            var patchDocument = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDocument);
            
            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_RetornaValidationProblem_CuandoHayErrorDeValidacion()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "Id"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var mensajeError = "Error de validación";
            controller.ModelState.AddModelError("", mensajeError);

            var patchDocument = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDocument);
            
            // Verificación
            var resultado = respuesta as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails!.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeError, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Patch_ActualizaUnCampo_CuandoSeLeEnviaUnaOperacion()
        {
            // Preparación
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Identificacion = "123",
                Foto = "URL-1"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>();
            controller.ObjectValidator = objectValidator;

            var patchDocument = new JsonPatchDocument<AutorPatchDTO>();
            patchDocument.Operations.Add(new Operation<AutorPatchDTO>("replace", "/nombres", null, "Juan2"));

            // Prueba
            var respuesta = await controller.Patch(1, patchDocument);
            
            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, actual: resultado!.StatusCode);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);

            var context2 = ConstruirContext(nombreBD);
            var autorBD = await context2.Autores.SingleAsync();

            Assert.AreEqual(expected: "Juan2", actual: autorBD.Nombres);
            Assert.AreEqual(expected: "Perez", actual: autorBD.Apellidos);
            Assert.AreEqual(expected: "123", actual: autorBD.Identificacion);
            Assert.AreEqual(expected: "URL-1", actual: autorBD.Foto);
        }

        [TestMethod]
        public async Task Delete_Retornar404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorExiste()
        {
            // Preparación
            var urlFoto = "URL-1";

            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor
            {
                Nombres = "Juan",
                Apellidos = "Perez",
                Foto = urlFoto
            });

            context.Autores.Add(new Autor
            {
                Nombres = "Pedro",
                Apellidos = "Jimenez"
            });

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificación
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, actual: resultado!.StatusCode);

            var context2 = ConstruirContext(nombreBD);
            var cantidad = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidad);

            var autor2Existe = await context2.Autores.AnyAsync(x => x.Nombres == "Pedro");
            Assert.IsTrue(autor2Existe);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Borrar(urlFoto, contenedor);
        }
    }
}
