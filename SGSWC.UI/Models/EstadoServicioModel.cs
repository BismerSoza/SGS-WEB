using Microsoft.AspNetCore.Mvc.Rendering;

namespace SGSWC.UI.Models
{
    public class EstadoServicioModel
    {
        public int IdReservacion { get; set; }

        public int IdServicio { get; set; }

        public string Servicio { get; set; } = "";

        public string Cliente { get; set; } = "";

        public DateTime FechaServicio { get; set; }

        public string EstadoActual { get; set; } = "";

        public int IdEstadoActual { get; set; }

        public List<SelectListItem> EstadosDisponibles { get; set; } = new();
    }

    public class HistorialEstadoServicioModel
    {
        public string EstadoAnterior { get; set; } = string.Empty;

        public string EstadoNuevo { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;

        public DateTime FechaCambio { get; set; }
    }

    public class CambiarEstadoServicioModel
    {
        public int IdReservacion { get; set; }

        public int IdEstadoNuevo { get; set; }

        public int IdUsuarioResponsable { get; set; }
    }
}
