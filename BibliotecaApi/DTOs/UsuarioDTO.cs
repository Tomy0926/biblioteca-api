using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.DTOs
{
    public class UsuarioDTO
    {
        [Required]
        public required string Email { get; set; }
        public DateTime FechaNacimiento { get; set; }
    }
}
