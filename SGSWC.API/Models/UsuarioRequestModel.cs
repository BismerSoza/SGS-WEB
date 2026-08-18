namespace SGSWC.API.Models
{
    public class ConsultarUsuariosRequestModel
    {
        public int Id_Usuario_Sesion { get; set; }
    }

    public class CambiarEstadoUsuarioRequestModel
    {
        public int Id_Usuario { get; set; }
        public bool Activo { get; set; }
        public int Id_Usuario_Sesion { get; set; }
    }

    public class UsuarioResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Nombre_Rol { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public bool Activo { get; set; }
        public bool DebeCambiarContrasena { get; set; }

    }

    public class GestionUsuarioResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public bool Activo { get; set; }
    }
}