namespace SGSWC.UI.Models
{
    public class ClienteFrecuenteEstadoModel
    {
        public int Id_Usuario { get; set; }
        public int Total_Servicios_30_Dias { get; set; }
        public bool Es_Cliente_Frecuente { get; set; }
    }
}
