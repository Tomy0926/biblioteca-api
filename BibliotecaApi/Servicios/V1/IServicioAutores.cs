using BibliotecaApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaApi.Servicios.V1
{
    public interface IServicioAutores
    {
        Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO);
    }
}