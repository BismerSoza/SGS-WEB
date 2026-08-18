namespace SGSWC.UI.Models
{
    public class ServicioPendienteUIModel
    {
        public int Id_Reservacion { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Nombre_Servicio { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Fecha_Creacion { get; set; } = string.Empty;
    }
}
