namespace SGSWC.API.Models
{
    public class DatosUsuarioRecuperacionResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Contrasena_hash { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }
        public bool CorreoVerificado { get; set; }
        public string? TokenRecuperacion { get; set; }
        public DateTime? TokenExpiracion { get; set; }
        public int IntentosFallidos { get; set; }
    }

    public class IntentoFallidoResponseModel
    {
        public int Resultado { get; set; }

    }
}
