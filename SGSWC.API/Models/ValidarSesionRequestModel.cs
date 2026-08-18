using System.ComponentModel.DataAnnotations;

namespace SGSWC.API.Models
{
    public class ValidarSesionRequestModel
    {
        [Required]
        public string Correo { get; set; } = string.Empty;
        [Required]
        public string Contrasena_hash { get; set; } = string.Empty;
    }
}
