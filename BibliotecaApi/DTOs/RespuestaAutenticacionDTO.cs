using System.ComponentModel.DataAnnotations;

namespace BibliotecaApi.DTOs
{
    public class RespuestaAutenticacionDTO
    {
        [Required]
        public required string Token { get; set; }
        public DateTime Expiracion { get; set; }
    }
}
