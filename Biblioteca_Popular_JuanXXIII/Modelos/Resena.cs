using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BibliotecaWeb.Models
{
    public class Resena
    {
        [Key]
        public int Id { get; set; }

        // --- Relaciones ---
        [Required]
        public int LibroId { get; set; }
        [ForeignKey("LibroId")]
        public Libro Libro { get; set; }

        [Required]
        public int SocioId { get; set; }
        [ForeignKey("SocioId")]
        public Socio Socio { get; set; }

        // --- Datos de la Reseña ---
        [Required(ErrorMessage = "Debes dejar una calificación")]
        [Range(1, 5, ErrorMessage = "La calificación debe ser entre 1 y 5 estrellas")]
        public int Calificacion { get; set; } // Las clásicas 1 a 5 estrellas

        [StringLength(1000, ErrorMessage = "El comentario no puede superar los 1000 caracteres")]
        public string? Comentario { get; set; }

        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        // Opcional: Para que los administradores puedan ocultar comentarios inapropiados
        public bool Aprobada { get; set; } = true;
    }
}