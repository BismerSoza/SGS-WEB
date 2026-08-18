namespace SGSWC.API.Models
{
    public class ReporteEstadisticasKpiModel
    {
        public int TotalResenas { get; set; }
        public decimal PromedioGeneral { get; set; }
        public decimal PromedioAnterior { get; set; }
        public decimal VariacionPromedio { get; set; }
        public int TotalPositivas { get; set; }
        public int TotalNegativas { get; set; }
        public decimal TasaSatisfaccion { get; set; }
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
    }

    public class DistribucionCalificacionModel
    {
        public int Calificacion { get; set; }
        public int Total { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class ResenaDetalleModel
    {
        public int IdResena { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreServicio { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class ReporteEstadisticasSatisfaccionResponseModel
    {
        public ReporteEstadisticasKpiModel Kpis { get; set; } = new();
        public List<DistribucionCalificacionModel> Distribucion { get; set; } = new();
        public List<ResenaDetalleModel> Resenas { get; set; } = new();
        public List<ServicioContratadoModel> Servicios { get; set; } = new();
    }

    public class ServicioContratadoModel
    {
        public string NombreServicio { get; set; } = string.Empty;
        public int TotalContrataciones { get; set; }
        public decimal PromedioCalificacion { get; set; }
    }
}
