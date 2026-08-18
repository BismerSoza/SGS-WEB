namespace SGSWC.UI.Models
{
    public class ReporteCalificacionesBajasKpiModel
    {
        public int TotalCalificacionesBajas { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public int TotalServiciosAfectados { get; set; }
        public int TotalClientesInsatisfechos { get; set; }
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
        public int Umbral { get; set; }
    }

    public class ServicioConQuejaModel
    {
        public int IdServicio { get; set; }
        public string NombreServicio { get; set; } = string.Empty;
        public int TotalQuejas { get; set; }
        public decimal PromedioCalificacion { get; set; }
        public int CalificacionMinima { get; set; }
    }

    public class CalificacionBajaDetalleModel
    {
        public int IdResena { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string CorreoCliente { get; set; } = string.Empty;
        public string NombreServicio { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class ReporteCalificacionesBajasResponseModel
    {
        public ReporteCalificacionesBajasKpiModel Kpis { get; set; } = new();
        public List<ServicioConQuejaModel> Servicios { get; set; } = new();
        public List<CalificacionBajaDetalleModel> Detalle { get; set; } = new();
    }
}
