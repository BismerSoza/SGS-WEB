namespace SGSWC.API.Models
{
    public class ActualizarRolRequestModel
    {
        public int IdUsuario { get; set; }
        public int Id_Rol { get; set; }
    }

    public class RegistrarAccesoRequestModel
    {
        public int? Id_Usuario { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string Tipo_Evento { get; set; } = string.Empty;
        public bool Exitoso { get; set; }
        public string? Ip { get; set; }
    }

    public class HistorialAccesoResponseModel
    {
        public int Id_Acceso { get; set; }
        public string Nombre_Usuario { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Tipo_Evento { get; set; } = string.Empty;
        public bool Exitoso { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string? Ip { get; set; }
    }

    public class ServicioComparadoResponseModel
    {
        public int Id_Servicio { get; set; }
        public string Nombre_Servicio { get; set; } = string.Empty;
        public int Total_Solicitado { get; set; }
        public decimal Porcentaje { get; set; }
    }

    public class NotificacionRequestModel
    {
        public int Id_Usuario { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
    }

    // HU-RE-008
    public class ClienteFrecuenteResponseModel
    {
        public int Id_Usuario { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int Total_Servicios { get; set; }
        public string Ultima_Reserva { get; set; } = string.Empty;
        public decimal Total_Gastado { get; set; }
    }

    // HU-RE-012
    public class HorarioSolicitadoResponseModel
    {
        public int Hora_Inicio { get; set; }
        public int Total_Reservas { get; set; }
        public decimal Porcentaje { get; set; }
    }
}