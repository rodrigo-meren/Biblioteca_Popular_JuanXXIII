using System;
using Biblioteca_Popular_JuanXXIII.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biblioteca_Popular_JuanXXIII      
{
    public class Prestamo
    {
        [Key]
        public int Id { get; set; }

        // --- Relaciones (Foreign Keys) ---
        [Required]
        public int SocioId { get; set; }
        [ForeignKey("SocioId")]
        public Socio Socio { get; set; } // Navegación hacia el lector

        [Required]
        public int LibroId { get; set; }
        [ForeignKey("LibroId")]
        public Libro Libro { get; set; } // Navegación hacia el libro físico

        // --- Fechas y Plazos ---
        [Required]
        public DateTime FechaPrestamo { get; set; } = DateTime.Now;

        [Required]
        public DateTime FechaVencimiento { get; set; } = DateTime.Now.AddDays(15); // Setea automáticamente los 15 días de base

        public DateTime? FechaDevolucion { get; set; } // Queda en NULL hasta que devuelven el libro

        // --- Lógica de Prórrogas (Autogestión) ---
        public int CantidadRenovaciones { get; set; } = 0; // Para contar cuántas veces prorrogó

        [StringLength(50)]
        public string Estado { get; set; } = "Vigente"; // Puede ser: Vigente, Vencido, Devuelto
    }
}