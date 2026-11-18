using Microsoft.AspNetCore.Antiforgery;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.Entidades
{
    public class Libro
    {
        public int Id { get; set; }
        [Required]
        [StringLength(250, MinimumLength =1)]
        public required string Titulo { get; set; }
        public List<AutorLibro> Autores { get; set; } = new List<AutorLibro>();
        public List<Comentario> Comentarios { get; set; } = new List<Comentario>();
    }
}
