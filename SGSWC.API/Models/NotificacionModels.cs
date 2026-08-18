namespace SGSWC.API.Models
{
    public class CambiarEstadoConNotificacionRequestModel
    {
        public int IdReservacion { get; set; }
        public int IdEstadoNuevo { get; set; }
        public int IdUsuarioAdmin { get; set; }
        public string? Motivo { get; set; }
    }

    public class CambiarEstadoConNotificacionResponseModel
    {
        public int Resultado { get; set; }
        public bool EmailEnviado { get; set; }
        public string MensajeNotificacion { get; set; } = string.Empty;
    }

    public class CambiarEstadoConNotificacionSPResult
    {
        public int Resultado { get; set; }
        public int Id_Usuario_Cliente { get; set; }
        public string? Correo_Cliente { get; set; }
        public string? Nombre_Cliente { get; set; }
        public string? Nombre_Estado_Anterior { get; set; }
        public string? Nombre_Estado_Nuevo { get; set; }
        public bool Notificaciones_Activas { get; set; }
    }
}