namespace SGSWC.API.Models
{
    public class PagoResponseModel
    {
        public int Id_Reservacion { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Nombre_Servicio { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado_Pago { get; set; } = string.Empty;
        public string Estado_Reservacion { get; set; } = string.Empty;
    }
}
