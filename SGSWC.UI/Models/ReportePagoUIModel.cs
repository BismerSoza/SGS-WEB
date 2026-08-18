namespace SGSWC.UI.Models
{
    public class ReportePagoUIModel
    {
        public int Id_Pago { get; set; }
        public int Id_Reservacion { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Nombre_Servicio { get; set; } = string.Empty;
        public string Metodo_Pago { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Fecha_Pago { get; set; } = string.Empty;

        public string Estado_Pago { get; set; } = string.Empty;
    }
}
