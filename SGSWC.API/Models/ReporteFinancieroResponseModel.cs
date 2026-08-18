namespace SGSWC.API.Models
{
    // HU-RE-007: Como administrador quiero generar reportes de ingresos y
    // egresos para evaluar la rentabilidad de la empresa.
    public class ReporteFinancieroResponseModel
    {
        public string Tipo { get; set; } = string.Empty;        // "Ingreso" | "Egreso"
        public string Fecha { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Referencia { get; set; } = string.Empty;
    }
}