namespace SGSWC.UI.Models
{
    // HU-GC-005: Analizar comportamiento de contratación de un cliente
    public class AnalisisHistorialClienteModel
    {
        public int Id_Usuario { get; set; }
        public int TotalServiciosContratados { get; set; }
        public decimal MontoTotalGenerado { get; set; }
        public decimal FrecuenciaContratacionDias { get; set; }
        public string? ServicioMasSolicitado { get; set; }
        public string? PeriodicidadContratacion { get; set; }
        public bool DatosSuficientes { get; set; }
        public string? Mensaje { get; set; }
    }
}