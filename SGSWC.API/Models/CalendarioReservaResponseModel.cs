namespace SGSWC.API.Models
{
    public class CalendarioReservaResponseModel
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

    public class ConflictoHorarioResponseModel
    {
        public int TotalConflictos { get; set; }
    }
}

