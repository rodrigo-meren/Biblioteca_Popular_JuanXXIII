using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII.Models;

namespace Biblioteca_Popular_JuanXXIII.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        
        public DbSet<Socio> Socios { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Resena> Resenas { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<Recordatorio> Recordatorios { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
    }
}