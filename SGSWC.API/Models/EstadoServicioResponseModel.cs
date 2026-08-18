namespace SGSWC.API.Models
{
    public class EstadoServicioResponseModel
    {
        public int IdReservacion { get; set; }

        public int IdServicio { get; set; }

        public string Servicio { get; set; } = string.Empty;

        public string Cliente { get; set; } = string.Empty;

        public DateTime FechaServicio { get; set; }

        public string EstadoActual { get; set; } = string.Empty;

        public int IdEstadoActual { get; set; }
    }
}
