namespace SGSWC.UI.Models
{
    public class EstadoSistemaModel
    {
        public double TiempoRespuestaPromedioMs { get; set; }
        public int SolicitudesActivas { get; set; }
        public double UmbralMs { get; set; }
        public string EstadoSistema { get; set; } = "Normal";
        public DateTime FechaConsulta { get; set; }
    }
}