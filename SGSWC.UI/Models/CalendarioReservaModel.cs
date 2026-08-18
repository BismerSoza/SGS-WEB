namespace SGSWC.UI.Models
{
    public class CalendarioReservaModel
    {
        public int IdReservacion { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreServicio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string DireccionServicio { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int? DuracionEstimadaMin { get; set; }
        public string? ObservacionesCliente { get; set; }
        public string? NombreEmpleado { get; set; }
        public string? IdEmpleado { get; set; }
    }

    public class EmpleadoDisponibilidadModel
    {
        public int IdEmpleado { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public bool Ocupado { get; set; }
    }

    public class AsignarEmpleadoRequest
    {
        public int IdReservacion { get; set; }
        public int IdEmpleado { get; set; }
    }

    public class AsignarEmpleadoResultado
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}