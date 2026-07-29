using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Cuota
    {
        [Key]
        public int Id { get; set; }

        // --- Relación con el Socie ---
        [Required]
        public int SocioId { get; set; }

        [ForeignKey("SocioId")]
        [ValidateNever]
        public Socio Socio { get; set; }

        // --- Datos Financieros ---
        [Required(ErrorMessage = "El monto es obligatorio.")]
        [Column(TypeName = "decimal(10,2)")] // Formato moneda (ej: 1500.50)
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "Falta el mes correspondiente.")]
        [StringLength(50)]
        public string MesCorrespondiente { get; set; } // Ej: "Marzo 2026" o "03-2026"

        // --- Fechas y Estados ---
        [Required]
        public DateTime FechaVencimiento { get; set; }

        public DateTime? FechaPago { get; set; } // Nulo hasta que pasen por caja

        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Puede ser: Pendiente, Pagada, Vencida, Anulada
    }
}