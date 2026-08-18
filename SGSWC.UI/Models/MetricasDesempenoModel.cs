namespace SGSWC.UI.Models
{
    // HU-RE-005: Generar métricas de desempeño para evaluar productividad
    public class MetricasDesempenoModel
    {
        public int TotalServiciosRealizados { get; set; }
        public decimal TiempoPromedioAtencionMin { get; set; }
        public decimal NivelCumplimiento { get; set; }
        public bool DatosDisponibles { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
    }
}