using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Libro
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El Código de Inventario es obligatorio")]
        [StringLength(50)]
        public string CodigoInventario { get; set; } // El código de barras físico

        [Required(ErrorMessage = "El Título es obligatorio")]
        [StringLength(255)]
        public string Titulo { get; set; }

        [Required(ErrorMessage = "El Autor es obligatorio")]
        [StringLength(150)]
        public string Autor { get; set; }

        [StringLength(150)]
        public string? Editorial { get; set; }

        public int? AnioPublicacion { get; set; }

        
        [StringLength(50)]
        public string? ISBN { get; set; }
        [MaxLength(500)]
        public string? UrlPortada { get; set; }
        [StringLength(50)]
        public string? UbicacionFisica { get; set; }
        [MaxLength(5000)]
        public string? Sinopsis { get; set; }
        public string? GeneroLiterario { get; set; }
        public bool Disponible { get; set; } = true;
        public bool EsDestacado { get; set; } = false;
        [Url(ErrorMessage = "Debe ser un enlace válido (empezar con http:// o https://)")]
        public string? PortadaUrl { get; set; }
    }
}