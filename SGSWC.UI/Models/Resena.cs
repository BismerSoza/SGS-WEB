namespace SGSWC.UI.Models
{
    public class Resena
    {
        public int Id { get; set; }
        public string NombreCliente { get; set; }
        public string Comentario { get; set; }
        public int Calificacion { get; set; }
        public DateTime Fecha { get; set; }

        // Nueva propiedad para identificar el servicio
        public string Servicio { get; set; }
    }
}
