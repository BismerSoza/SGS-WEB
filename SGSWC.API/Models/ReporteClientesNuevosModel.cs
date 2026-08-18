namespace SGSWC.API.Models
{
    // Models/ReporteClientesNuevosModel.cs
    public class ReporteClientesNuevosKpiModel
    {
        public int TotalPeriodo { get; set; }
        public int TotalAnterior { get; set; }
        public decimal Variacion { get; set; }
        public int Activos { get; set; }
        public string FechaInicio { get; set; } = string.Empty;
        public string FechaFin { get; set; } = string.Empty;
    }

    public class ClienteNuevoDetalleModel
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string FechaRegistro { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int TotalReservas { get; set; }
    }

    public class ReporteClientesNuevosResponseModel
    {
        public ReporteClientesNuevosKpiModel Kpis { get; set; } = new();
        public List<ClienteNuevoDetalleModel> Clientes { get; set; } = new();
    }
}
