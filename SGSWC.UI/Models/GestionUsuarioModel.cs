namespace SGSWC.UI.Models
{
    public class GestionUsuarioModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Nombre_Rol { get; set; } = string.Empty;
        public int Id_Rol { get; set; }
        public bool Activo { get; set; }
        public DateTime Fecha_Creacion { get; set; }
    }
}