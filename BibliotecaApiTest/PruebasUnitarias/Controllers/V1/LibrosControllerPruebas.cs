using BibliotecaApi.Controllers.V1;
using BibliotecaApi.DTOs;
using BibliotecaApiTest.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BibliotecaApiTest.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class LibrosControllerPruebas: BasePruebas
    {
        [TestMethod]
        public async Task Get_RetornarCeroLibros_CuandoNoHayLibros()
        {
            // Preparacion
            var nombreDB = Guid.NewGuid().ToString();
            var context = ConstruirContext(nombreDB);
            var mapper = ConfigurarAutoMapper();
            IOutputCacheStore outputCacheStore = null!;

            var controller = new LibrosController(context, mapper, outputCacheStore);

            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var paginacionDTO = new PaginacionDTO(1, 1);

            // Prueba

            var respuesta = await controller.Get(paginacionDTO);

            // Verificacion
            Assert.AreEqual(expected: 0, actual: respuesta.Count());
        }
    }
}
