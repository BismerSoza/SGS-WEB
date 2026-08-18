namespace SGSWC.UI.Models
{
    public class ReporteServiciosModel
    {
        public int IdReservacion { get; set; }
        public string FechaReserva { get; set; } = string.Empty;
        public string HoraReserva { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string CorreoCliente { get; set; } = string.Empty;
        public string TelefonoCliente { get; set; } = string.Empty;
        public string NombreServicio { get; set; } = string.Empty;
        public decimal PrecioBase { get; set; }
        public decimal TotalReservacion { get; set; }
        public string DireccionServicio { get; set; } = string.Empty;
        public string EstadoReservacion { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
        public string NombreEmpleado { get; set; } = string.Empty;
        public string EstadoPago { get; set; } = string.Empty;
    }
}
