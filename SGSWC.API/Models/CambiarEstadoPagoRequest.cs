namespace SGSWC.API.Models
{
    public class CambiarEstadoPagoRequest
    {
        public int Id_Reservacion { get; set; }
        public string Estado_Pago_Nuevo { get; set; } = "pagado";
        public int? Id_Metodo { get; set; }
        public string? Referencia_Externa { get; set; }
    }
}
