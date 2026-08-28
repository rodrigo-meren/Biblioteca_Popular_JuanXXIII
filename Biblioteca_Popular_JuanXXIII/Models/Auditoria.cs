using System;
using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Auditoria
    {
        [Key]
        public int Id { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UsuarioResponsable { get; set; } // Quién hizo la acción (ej: DNI o Email)

        [StringLength(50)]
        public string Modulo { get; set; } // Ej: "Cuotas", "Socios", "Libros", "Noticias"

        [StringLength(50)]
        public string Accion { get; set; } // Ej: "Cobro", "Alta", "Baja", "Edición"

        [StringLength(255)]
        public string Detalles { get; set; } // Ej: "Se cobró la cuota de Agosto al socio Juan Pérez"
    }
}