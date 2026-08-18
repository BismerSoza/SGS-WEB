namespace SGSWC.API.Models
{
    public class HistorialEstadoServicioResponseModel
    {
        public string EstadoAnterior { get; set; } = string.Empty;
        public string EstadoNuevo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
    }
}
