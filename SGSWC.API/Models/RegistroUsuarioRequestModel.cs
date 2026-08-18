using System.ComponentModel.DataAnnotations;

namespace SGSWC.API.Models
{
    public class RegistroUsuarioRequestModel
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;
        [Required]
        public string Correo { get; set; } = string.Empty;
        [Required]
        public string Contrasena_hash { get; set; } = string.Empty;
    }
}
