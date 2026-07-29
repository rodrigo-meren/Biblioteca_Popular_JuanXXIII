using Biblioteca_Popular_JuanXXIII.Data;
using Biblioteca_Popular_JuanXXIII.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
// Faltaban estas tres para el CSV:
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Biblioteca_Popular_JuanXXIII.Pages.Libros;

[AllowAnonymous]
public class InicioModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public InicioModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // 1. La única lista que necesitamos es la de Libros
    public IList<Libro> Libro { get; set; } = default!;

    // --- PROPIEDADES PARA BUSCAR Y PAGINAR ---
    [BindProperty(SupportsGet = true)]
    public string Buscar { get; set; }

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
        IQueryable<Libro> consulta = _context.Libros;

        if (!string.IsNullOrEmpty(Buscar))
        {
            consulta = consulta.Where(l => l.Titulo.Contains(Buscar) ||
                                           l.Autor.Contains(Buscar) ||
                                           l.GeneroLiterario.Contains(Buscar) ||
                                           l.ISBN.Contains(Buscar));
        }

        var cantidadTotal = await consulta.CountAsync();
        TotalPaginas = (int)Math.Ceiling(cantidadTotal / (double)RegistrosPorPagina);

        if (PaginaActual < 1) PaginaActual = 1;
        if (PaginaActual > TotalPaginas && TotalPaginas > 0) PaginaActual = TotalPaginas;

        // Guardamos en "Libro" y ordenamos por "Titulo" (no por apellido)
        Libro = await consulta
            .OrderBy(l => l.Titulo)
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