namespace SGSWC.UI.Models
{
    public class Reseña
    {
        public int Id { get; set; }
        public string Servicio { get; set; }
        public string Cliente { get; set; } // Obligatorio (si no escribe nada se guarda "Anónimo")
        public int Calificacion { get; set; } // Obligatorio (1 a 5 estrellas)
        public string Comentario { get; set; } // Opcional
        public string ImagenPath { get; set; } // Opcional
    }
}
