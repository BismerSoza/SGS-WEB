namespace SGSWC.API.Models
{
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
        public string HoraInicio { get; set; } = string.Empty;
        public int DuracionMin { get; set; }
        public string EstadoAsignacion { get; set; } = "pendiente";
        public string Observaciones { get; set; } = string.Empty;
    }
}

