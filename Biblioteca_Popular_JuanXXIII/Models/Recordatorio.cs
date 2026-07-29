using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Recordatorio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El texto es obligatorio.")]
        [StringLength(200)]
        public string Texto { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool EsUrgente { get; set; } = false;

        public bool Resuelto { get; set; } = false;
    }
}