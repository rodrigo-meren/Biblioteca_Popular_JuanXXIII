using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
// Faltaban estas tres para el CSV:
using CsvHelper;
using CsvHelper.Configuration;
using MercadoPago.Resource.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace Biblioteca_Popular_JuanXXIII.Pages.Libros;

[AllowAnonymous]
public class InicioModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public InicioModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public string FiltroGenero { get; set; }
    public SelectList GenerosDisponibles { get; set; }
    public IList<Libro> Libro { get; set; } = default!;

    // --- PROPIEDADES PARA BUSCAR Y PAGINAR ---
    [BindProperty(SupportsGet = true)]
    public string BuscarTexto { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PaginaActual { get; set; } = 1;

    public int TotalPaginas { get; set; }
    public int RegistrosPorPagina { get; set; } = 20;
    // --- MÉTODO PARA VACIAR TODO EL CATÁLOGO ---
    public async Task<IActionResult> OnPostVaciarCatalogoAsync()
    {
        // 1. Buscamos todos los libros que existen en la base
        var todosLosLibros = await _context.Libros.ToListAsync();

        if (todosLosLibros.Any())
        {
            // 2. Le decimos al sistema que los borre todos
            _context.Libros.RemoveRange(todosLosLibros);

            // 3. Guardamos los cambios
            await _context.SaveChangesAsync();
        }

        // 4. Recargamos la página limpia
        return RedirectToPage("./Inicio");
    }

    // --- 2. MÉTODO PRINCIPAL (ON GET) UNIFICADO ---
    public async Task OnGetAsync()
    {
        // A. Consultamos todos los géneros únicos que existen para llenar el DDL
        var generos = await _context.Libros
            .Select(l => l.GeneroLiterario)
            .Distinct()
            .ToListAsync();

        // Limpiamos los vacíos y ordenamos alfabéticamente
        generos.RemoveAll(string.IsNullOrWhiteSpace);
        GenerosDisponibles = new SelectList(generos.OrderBy(g => g));

        // B. Empezamos a armar la consulta base de libros
        IQueryable<Libro> consulta = _context.Libros;

        // C. Filtro 1: Buscador de texto
        if (!string.IsNullOrEmpty(BuscarTexto))
        {
            // 1. Pasamos lo que escribió el usuario a minúsculas
            var terminoBusqueda = BuscarTexto.ToLower();

            // 2. Comparamos convirtiendo también los campos de la base a minúsculas
            //    y agregamos el CodigoInventario a la búsqueda
            consulta = consulta.Where(l => l.Titulo.ToLower().Contains(terminoBusqueda) ||
                                           l.Autor.ToLower().Contains(terminoBusqueda) ||
                                           l.GeneroLiterario.ToLower().Contains(terminoBusqueda) ||
                                           l.ISBN.ToLower().Contains(terminoBusqueda) ||
                                           l.CodigoInventario.ToLower().Contains(terminoBusqueda));
        }
        // D. Filtro 2: Menú desplegable de Género
        if (!string.IsNullOrEmpty(FiltroGenero))
        {
            consulta = consulta.Where(l => l.GeneroLiterario == FiltroGenero);
        }
        consulta = consulta.OrderBy(l => l.CodigoInventario.Length).ThenBy(l => l.CodigoInventario);

        // E. Paginación Matemática
        var cantidadTotal = await consulta.CountAsync();

        TotalPaginas = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPagina);

        if (PaginaActual < 1) PaginaActual = 1;
        if (PaginaActual > TotalPaginas && TotalPaginas > 0) PaginaActual = TotalPaginas;

        // F. Guardamos en "Libro" y ejecutamos la consulta a la base de datos
        Libro = await consulta
            .Skip((PaginaActual - 1) * RegistrosPorPagina)
            .Take(RegistrosPorPagina)
            .ToListAsync();
    }


    // --- 3. MÉTODO PARA IMPORTAR EL CSV (Faltaba) ---
    public async Task<IActionResult> OnPostImportarAsync(IFormFile archivoCsv)
    {
        if (archivoCsv == null || archivoCsv.Length == 0) return RedirectToPage();

        var config = new CsvConfiguration(new CultureInfo("es-AR"))
        {
            Delimiter = "\t",
            HasHeaderRecord = true,

            // 1. Evita que explote si encuentra un formato raro
            BadDataFound = null,


            Quote = '\0'
        };

        using (var stream = new StreamReader(archivoCsv.OpenReadStream()))
        using (var csv = new CsvReader(stream, config))
        {
            csv.Context.RegisterClassMap<LibroMap>();
            var records = csv.GetRecords<Libro>().ToList();

            foreach (var libro in records)
            {
                var existeLibro = await _context.Libros.AnyAsync(l => l.Titulo == libro.Titulo);
                if (!existeLibro)
                {
                    _context.Libros.Add(libro);
                }
            }
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Inicio");
    }


    // --- 4. MÉTODO PARA BORRAR ---
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var libro = await _context.Libros.FindAsync(id);
        if (libro != null)
        {
            _context.Libros.Remove(libro);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage("./Inicio");
    }


    // --- 5. MÉTODO PARA SINCRONIZAR LA API ---
    public async Task<IActionResult> OnPostSincronizarLoteAsync()
    {
        var librosFaltantes = await _context.Libros
            .Where(l => !string.IsNullOrEmpty(l.ISBN) && string.IsNullOrEmpty(l.UrlPortada))
            .Take(10)
            .ToListAsync();

        if (!librosFaltantes.Any()) return RedirectToPage();

        string apiKey = "AIzaSyCeel9dCx0FA-VNewgi2ojNXNqtinW_pYo"; // Tu clave está perfecta
        using var httpClient = new HttpClient();

        foreach (var libro in librosFaltantes)
        {
            string isbnLimpio = libro.ISBN.Replace("-", "").Trim();
            string urlGoogle = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbnLimpio}&key={apiKey}";

            try
            {
                var respuesta = await httpClient.GetAsync(urlGoogle);
                if (respuesta.IsSuccessStatusCode)
                {
                    var jsonString = await respuesta.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(jsonString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("totalItems", out var totalItems) && totalItems.GetInt32() > 0)
                    {
                        var infoLibro = root.GetProperty("items")[0].GetProperty("volumeInfo");

                        if (infoLibro.TryGetProperty("description", out var descripcion))
                        {
                            libro.Sinopsis = descripcion.GetString();
                        }
                        if (infoLibro.TryGetProperty("categories", out var categorias) && categorias.GetArrayLength() > 0)
                        {
                            libro.GeneroLiterario = categorias[0].GetString();
                        }
                        if (infoLibro.TryGetProperty("imageLinks", out var imagenes))
                        {
                            if (imagenes.TryGetProperty("thumbnail", out var foto))
                            {
                                libro.UrlPortada = foto.GetString()?.Replace("http://", "https://");
                            }
                        }
                    }
                }
            }
            catch { }

            await Task.Delay(1000); // 1 segundo de reposo para Google
        }
        await _context.SaveChangesAsync();
        return RedirectToPage("./Inicio");
    }
    public async Task<IActionResult> OnPostReservarAsync(int libroId)
    {
        // 1. Identificamos al socie que hizo clic
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var socio = await _context.Socios.FirstOrDefaultAsync(s => s.IdentityUserId == userId);

        if (socio == null) return RedirectToPage("/Account/Logout", new { area = "Identity" });

        // 2. Buscamos el libro que quiere reservar
        var libro = await _context.Libros.FindAsync(libroId);
        if (libro == null) return NotFound();

        // 3. (Seguridad) Verificamos que no haya reservado este mismo libro antes y siga pendiente
        var reservaExistente = await _context.Reservas
            .AnyAsync(r => r.LibroId == libroId && r.SocioId == socio.Id && (r.Estado == "Pendiente de retiro" || r.Estado == "En espera"));

        if (reservaExistente)
        {
            TempData["MensajeError"] = "Ya tenés una reserva activa o estás en lista de espera para este libro.";
            return RedirectToPage();
        }

        // 4. Creamos la nueva reserva
        var nuevaReserva = new Reserva
        {
            LibroId = libro.Id,
            SocioId = socio.Id,
            FechaSolicitud = DateTime.Now
        };

        if (libro.Disponible) // Evaluamos el booleano
        {
            nuevaReserva.Estado = "Pendiente de retiro";
            nuevaReserva.FechaLimiteRetiro = DateTime.Now.AddHours(48);

            // Lo pasamos a false para que desaparezca de la disponibilidad
            libro.Disponible = false;

            TempData["MensajeExito"] = $"¡Reserva confirmada! Tenés tiempo hasta el {nuevaReserva.FechaLimiteRetiro.Value.ToString("dd/MM HH:mm")} hs para pasar a retirarlo.";
        }
        else
        {
            nuevaReserva.Estado = "En espera";
            nuevaReserva.FechaLimiteRetiro = null;

            TempData["MensajeExito"] = "El libro no está disponible actualmente. Quedaste en la lista de espera y te vamos a avisar cuando lo devuelvan.";
        }

        _context.Reservas.Add(nuevaReserva);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostAlternarDestacadoAsync(int id)
    {
        var libro = await _context.Libros.FindAsync(id);

        if (libro != null)
        {
            // Si el libro NO está destacado actualmente (es decir, el usuario quiere destacarlo)
            if (!libro.EsDestacado)
            {
                // Contamos cuántos libros ya tienen estrella
                int cantidadDestacados = await _context.Libros.CountAsync(l => l.EsDestacado);

                if (cantidadDestacados >= 12)
                {
                    // Frenamos la operación y le mandamos un mensaje de error
                    TempData["MensajeError"] = "Ya tenés 12 libros destacados en la portada. Por favor, quitale la estrella a uno antes de destacar otro.";
                    return RedirectToPage(); // Cortamos la ejecución acá
                }
            }

            // Si pasó la validación (o si estaba quitando una estrella), invertimos el estado
            libro.EsDestacado = !libro.EsDestacado;
            await _context.SaveChangesAsync();

            // Mensaje de éxito
            if (libro.EsDestacado)
            {
                TempData["MensajeExito"] = $"El libro '{libro.Titulo}' ahora aparece en la portada.";
            }
            else
            {
                TempData["MensajeExito"] = $"El libro '{libro.Titulo}' se quitó de las recomendaciones.";
            }
        }

        return RedirectToPage();
    }
    

    // --- 6. CLASE MAPEADORA PARA EL EXCEL (Faltaba) ---
    public class LibroMap : ClassMap<Libro>
    {
        public LibroMap()
        {
            Map(m => m.CodigoInventario).Name("Codigo_Inventario").Optional();
            Map(m => m.Titulo).Name("Titulo").Optional();
            Map(m => m.Autor).Name("Autor").Optional();


            Map(m => m.ISBN).Name("ISBN", "I SBN").Optional();

            Map(m => m.Editorial).Name("Editorial").Optional();
        }


    }
}