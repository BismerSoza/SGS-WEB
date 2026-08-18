namespace SGSWC.API.Models
{
    public class CrearReservaRequestModel
    {
        public int Id_Usuario { get; set; }
        public int Id_Servicio { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    public class ServicioResponseModel
    {
        public int Id_Servicio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio_Base { get; set; }

        public string? Imagen_Ruta { get; set; }

    }

    public class ReservaResponseModel
    {
        public int Id_Reservacion { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Direccion_Servicio { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Fecha_Creacion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class CancelarRequestModel
    {
        public int Id_Reservacion { get; set; }
        public int Id_Usuario { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? Detalle { get; set; }
    }

    public class ModificarFechaRequestModel
    {
        public int Id_Reservacion { get; set; }
        public int Id_Usuario { get; set; }
        public string Nueva_Fecha { get; set; } = string.Empty;
        public string Nueva_Hora { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
    public class PagoRequestModel
    {
        public int Id_Reservacion { get; set; }
        public int Id_Usuario { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class EstadoPagoResponseModel
    {
        public int Id_Reservacion { get; set; }
        public string Estado_Pago { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Correo_Cliente { get; set; } = string.Empty;
    }
}