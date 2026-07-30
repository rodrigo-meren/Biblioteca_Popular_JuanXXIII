using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Noticia
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(150, ErrorMessage = "El título no puede superar los 150 caracteres")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio")]
        public string Contenido { get; set; } = string.Empty;

        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        // Para poder ocultar noticias viejas sin tener que borrarlas de la base de datos
        public bool Activa { get; set; } = true;
    }
}