using AutoMapper;
using BibliotecaApi.Datos;
using BibliotecaApi.DTOs;
using BibliotecaApi.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaApi.Controllers.V2
{
    [ApiController]
    [Route("api/v2/autores-coleccion")]
    [Authorize(Policy = "esadmin")]
    public class AutoresColeccioncontroller : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        public AutoresColeccioncontroller(ApplicationDbContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet("{ids}", Name = "ObtenerAutoresPorIdsV2")]
        public async Task<ActionResult<List<AutorConLibrosDTO>>> Get( string ids) //api/autores-coleccion/1,2,3
        {
            var idsColeccion = new List<int>();

            foreach (var id in ids.Split(","))
            {
                if (int.TryParse(id, out int idInt))
                {
                    idsColeccion.Add(idInt);
                }
                else
                {
                    return BadRequest($"El valor '{id}' no es un ID válido.");
                }
            }

            if (!idsColeccion.Any())
            {
                ModelState.AddModelError(nameof(ids), "Debe proporcionar al menos un ID válido.");
                return ValidationProblem();
            }

            var autores =  await context.Autores
                .Include(x => x.Libros)
                .ThenInclude(x => x.Libro)
                .Where(x => idsColeccion.Contains(x.Id)).ToListAsync();

            if (autores.Count != idsColeccion.Count)
            {
                return NotFound();
            }

            var autoresDTO = mapper.Map<List<AutorConLibrosDTO>>(autores);
            return Ok(autoresDTO);
        }

        [HttpPost]
        public async Task<ActionResult> Post(IEnumerable<AutorCreacionDTO> autoresCreacionDTO)
        {
            var autores = mapper.Map<IEnumerable<Autor>>(autoresCreacionDTO);
            context.AddRange(autores);
            await context.SaveChangesAsync();

            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            var ids = autores.Select(x => x.Id);
            var idsString = string.Join(",", ids);

            return CreatedAtRoute("ObtenerAutoresPorIdsV2", new { ids = idsString }, autoresDTO);
        }
    }
}
