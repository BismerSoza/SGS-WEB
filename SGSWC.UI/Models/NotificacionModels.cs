namespace SGSWC.UI.Models
{
    public class CambiarEstadoConNotificacionModel
    {
        public int IdReservacion { get; set; }
        public int IdEstadoNuevo { get; set; }
        public int IdUsuarioAdmin { get; set; }
        public string? Motivo { get; set; }
    }

    public class CambiarEstadoConNotificacionRespuestaModel
    {
        public int Resultado { get; set; }
        public bool EmailEnviado { get; set; }
        public string MensajeNotificacion { get; set; } = string.Empty;
    }
}