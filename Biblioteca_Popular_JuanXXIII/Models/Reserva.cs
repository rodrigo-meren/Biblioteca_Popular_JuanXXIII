using System.ComponentModel.DataAnnotations;

namespace Biblioteca_Popular_JuanXXIII.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }

        public int LibroId { get; set; }
        public Libro Libro { get; set; } = default!;

        public int SocioId { get; set; }
        public Socio Socio { get; set; } = default!;

        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        // Fecha límite para ir a buscarlo (será NULL si el libro está prestado y queda en lista de espera)
        public DateTime? FechaLimiteRetiro { get; set; }

        // Estados posibles: "Pendiente de retiro" (48hs), "En espera" (sin stock), "Completada" (lo retiró), "Vencida" (no lo fue a buscar)
        public string Estado { get; set; } = "Pendiente";
    }
}