namespace SGSWC.UI.Models
{
    public class ClienteDetalleModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion_Principal { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string Nombre_Rol { get; set; } = string.Empty;
    }

    public class HistorialServicioClienteModel
    {
        public int Id_Reservacion { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Fecha_Creacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Nombre_Servicio { get; set; } = string.Empty;
        public string Tipo_Servicio { get; set; } = string.Empty;
    }

    public class PerfilClienteViewModel
    {
        public ClienteDetalleModel Cliente { get; set; } = new();
        public ClienteFrecuenteEstadoModel EstadoFrecuente { get; set; } = new();
        public List<HistorialServicioClienteModel> Historial { get; set; } = new();
    }
}