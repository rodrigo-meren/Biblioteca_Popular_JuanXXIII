using Biblioteca_Popular_JuanXXIII.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII  
{
    public class Socio
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El DNI o Carnet es obligatorio")]
        [StringLength(50)]
        public string DniCarnet { get; set; }

        [Required(ErrorMessage = "El Nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El Apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "La Fecha de Nacimiento es obligatoria para las estadísticas")]
        public DateTime FechaNacimiento { get; set; }

        [StringLength(50)]
        public string Genero { get; set; } // Flexible para opciones inclusivas
        public string? Pronombre { get; set; }
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [StringLength(150)]
        public string? Correo { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(50)]
        public string Categoria { get; set; }
       
        public bool Activo { get; set; }
        // --- Relación con Cuotas ---
        public ICollection<Cuota>? Cuotas { get; set; }
        public string? IdentityUserId { get; set; }
    }
}