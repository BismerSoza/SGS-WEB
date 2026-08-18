namespace SGSWC.API.Models
{
    public class RegistrarResenaRequestModel
    {
        public int Id_Reservacion { get; set; }
        public int Id_Usuario { get; set; }
        public int Calificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
    }

    /// <summary>Mapea el result set de RegistrarResena (resultado + mensaje).</summary>
    public class RegistrarResenaResultModel
    {
        public int Resultado { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ResenaPorServicioResponseModel
    {
        public int Id_Resena { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
        public string? Respuesta_Admin { get; set; }
    }

    public class ResenaPorUsuarioResponseModel
    {
        public int Id_Resena { get; set; }
        public int Id_Reservacion { get; set; }
        public int Calificacion { get; set; }
        public string? Comentario { get; set; }
        public DateTime Fecha { get; set; }
        public string? Respuesta_Admin { get; set; }
    }
}