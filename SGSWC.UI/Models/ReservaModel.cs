namespace SGSWC.UI.Models
{
    public class ReservaModel
    {
        public int Id_Usuario { get; set; }
        public int Id_Servicio { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public string? Observaciones { get; set; }

        public int Id_Reservacion { get; set; }
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string Fecha_Creacion { get; set; } = string.Empty;
        public string Estado_Pago { get; set; } = string.Empty;
    }

    public class ServicioModel
    {
        public int Id_Servicio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio_Base { get; set; }

        public string? Imagen_Ruta { get; set; }
    }

    /// <summary>
    /// HU-C-004: combina una reserva Completada del cliente con su reseña
    /// (si ya la dejó), para mostrar en la vista de Reseñas.
    /// </summary>
    public class ReservaConResenaModel
    {
        public int Id_Reservacion { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public decimal Total { get; set; }

        public bool TieneResena { get; set; }
        public int? Calificacion { get; set; }
        public string? Comentario { get; set; }
        public string? Respuesta_Admin { get; set; }
    }

    /// <summary>Mapea ResenaPorUsuarioResponseModel de la API.</summary>
    public class ResenaUsuarioModel
    {
        public int Id_Resena { get; set; }
        public int Id_Reservacion { get; set; }
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
        public string? Respuesta_Admin { get; set; }
    }
    public class EstadoPagoUIModel
    {
        public int Id_Reservacion { get; set; }
        public string Estado_Pago { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Correo_Cliente { get; set; } = string.Empty;
    }

    public class PayPalOrdenUIModel
    {
        public string OrderId { get; set; } = string.Empty;
        public string ApprovalUrl { get; set; } = string.Empty;
    }
}