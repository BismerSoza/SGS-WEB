namespace SGSWC.API.Models
{
    public class CambiarEstadoServicioRequestModel
    {
        public int IdReservacion { get; set; }
        public int IdEstadoNuevo { get; set; }
        public int IdUsuarioResponsable { get; set; }
    }
}
