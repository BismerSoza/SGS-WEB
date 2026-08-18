namespace SGSWC.UI.Models
{
    public class DashboardModel
    {
        public int TotalServiciosMes { get; set; }
        public decimal TotalIngresosMes { get; set; }
        public int ServiciosPendientes { get; set; }
        public int ClientesFrecuentes { get; set; }
        public decimal VariacionServicios { get; set; }
        public decimal VariacionIngresos { get; set; }
        public int CanceladosMes { get; set; }
        public decimal TasaCancelacion { get; set; }
        public string ServicioTop { get; set; } = string.Empty;
        public decimal PromedioIngresoPorServicio { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
    }
}
