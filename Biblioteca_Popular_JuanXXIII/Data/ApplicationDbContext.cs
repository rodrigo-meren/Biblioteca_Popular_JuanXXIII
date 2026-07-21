using Microsoft.EntityFrameworkCore;
using Biblioteca_Popular_JuanXXIII.Models;

namespace Biblioteca_Popular_JuanXXIII.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Estas van a ser tus tablas en SQL Server
        public DbSet<Socio> Socios { get; set; }
        public DbSet<Libro> Libros { get; set; }
        public DbSet<Prestamo> Prestamos { get; set; }
        public DbSet<Resena> Resenas { get; set; }
    }
}