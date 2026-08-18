namespace SGSWC.API.Models
{
    public class DatosUsuarioResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Contrasena_hash { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        public bool DebeCambiarContrasena { get; set; }
        public bool Bloqueado { get; set; }
        public int IntentosFallidos { get; set; }
        public bool NotificacionesActivas { get; set; }

    }
}
