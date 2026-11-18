using AutoMapper;
using BibliotecaApi.Datos;
using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using BibliotecaApi.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

namespace BibliotecaApi.Controllers.V2
{
    [ApiController]
    [Route("api/v2/libros")]
    [Authorize(Policy = "esadmin")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cache = "libros-obtener";

        public LibrosController(ApplicationDbContext context, IMapper mapper, IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
        }

        //Consultar libros
        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<LibroDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            var queryable = context.Libros.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var libros = await queryable
                .OrderBy(x => x.Titulo)
                .Paginar(paginacionDTO).ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);
            return librosDTO;
        }

        //Consultar un libro por id
        [HttpGet("{id:int}", Name = "ObtenerLibroV2")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<LibroConAutoresDTO>> Get(int id)
        {
            var libro = await context.Libros
                .Include(x => x.Autores)
                .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (libro is null)
            {
                return NotFound();
            }

            var libroDTO = mapper.Map<LibroConAutoresDTO>(libro);
            return libroDTO;
        }

        // Agregar libros
        [HttpPost]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult<LibroDTO>> Post(LibroCreacionDTO libroCreacionDTO)
        {
            var libro = mapper.Map<Libro>(libroCreacionDTO);
            AsignarOrdenAutores(libro);

            context.Libros.Add(libro);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            var libroDTO = mapper.Map<LibroDTO>(libro);
            return CreatedAtRoute("ObtenerLibroV2", new { id = libro.Id }, libroDTO);
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores != null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i;
                }
            }
        }

        //// Actualizar libros
        [HttpPut("{id:int}")]
        [ServiceFilter<FiltroValidacionLibro>()]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            var LibroDb = await context.Libros
                .Include(x => x.Autores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (LibroDb is null)
            {
                return NotFound();
            }

            LibroDb = mapper.Map(libroCreacionDTO, LibroDb);
            AsignarOrdenAutores(LibroDb);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            return NoContent();
        }

        // Borrar libros
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var librosBorrados = await context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (librosBorrados == 0)
            {
                return NotFound();
            }

            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();

        }
    }
}
